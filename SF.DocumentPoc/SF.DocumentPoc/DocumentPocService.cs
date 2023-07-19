using Newtonsoft.Json;
using OfficeOpenXml;
using SF.DocumentPoc.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace SF.DocumentPoc
{
    public class DocumentPocService
    {
        private static string IronPdfLicenseText = "To extract all of the page text, obtain a license key from https://ironpdf.com/licensing/";

        private readonly HttpClient _httpClient;
        string DocBotApiBaseUrl;
        string DocumentBotId;
        private readonly string NameEntityId = "fba3155d-01cd-4698-a6d9-d4dc6640adce";
        int DocumentCount = 0;
        string TemplateFilepath;
        string ExternalApiEndpointUrl;
        string ApiKey;

        public DocumentPocService(int envChoice, string accessToken, string documentbotId, int noOfDocuments, string externalApiEndpointUrl, string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("authorization", accessToken);
            DocumentBotId = documentbotId;
            DocumentCount = noOfDocuments;
            ExternalApiEndpointUrl = externalApiEndpointUrl;
            ApiKey = apiKey;
            switch (envChoice)
            {
                case 1:
                    DocBotApiBaseUrl = "https://dev.simplifai.ai/da/api/documentbot";
                    break;
                case 2:
                    DocBotApiBaseUrl = "https://qa.simplifai.ai/da/api/documentbot";
                    break;
                case 3:
                    DocBotApiBaseUrl = "https://staging.simplifai.ai/da/api/documentbot";
                    break;
            }
        }

        public async Task ReadFromExcel()
        {
            string filePath = @"D:\\Others\\SF-Test\\DocumentPocGit\\Documents\\DataExcel.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excelPackage = new ExcelPackage(new FileInfo(filePath));
            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

            for(int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var textToReplace = worksheet.Cells[row, 1].Value?.ToString();
                TemplateFilepath = worksheet.Cells[row, 2].Value?.ToString();
                var documentIds = new List<Guid>();

                for(int i = 0; i < DocumentCount; i++)
                {
                    var docuementId = await GenerateDocAndSendToBot(textToReplace, i+1);
                    documentIds.Add(docuementId);

                    if(i == 99 || i == DocumentCount - 1)
                    {
                        await MarkDocumentsAsCompleted(documentIds);
                        documentIds.Clear();
                    }
                }
                //for(int column = 1; column < worksheet.Dimension.Columns; column++)
                //{
                //    var name = worksheet.Cells[row, column].Value?.ToString();

                //    for(int i = 0; i < DocumentCount; i++)
                //    {

                //    }
                //}
            }
        }

        public async Task<Guid> GenerateDocAndSendToBot(string textToReplace, int documentNo)
        {
            var sw = Stopwatch.StartNew();

            using var PDFDocument = PdfDocument.FromFile(TemplateFilepath);
            string AllText = CleanTextAndHtmlNewLineCharacters(PDFDocument.ExtractAllText());

            string replaceWith = GenerateRandomString(textToReplace.Length, false);
            AllText = AllText.Replace(textToReplace, replaceWith);
            var renderer = new ChromePdfRenderer();
            var pdf = renderer.RenderHtmlAsPdf(AllText);

            var documentName = $"SeventhTestRun_{documentNo}.pdf";
            await using var ms = new MemoryStream(pdf.BinaryData);
            var botResponse = await SendDocumentToBot(pdf.BinaryData, documentName);

            var documentSearchRequestDto = new GetDocumentRequestDto()
            {
                SearchText = documentName,
                StartDate = DateTime.ParseExact("1999-12-31T18:30:00.000Z", "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                EndDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = 10,
            };

            var searchResult = await SearchForDocumentId(JsonConvert.SerializeObject(documentSearchRequestDto));
            var documentId = searchResult.Result.Records[0].Id;
            var documentDetails = await GetDocumentDetails(documentId);

            var entities = new List<DocumentEntityTaggedReadDto>();
            var intents = new List<DocumentIntentTaggedReadDto>();

            var wordIds = new List<int>();

            wordIds.Add(documentDetails.Result.DocumentJson.Pages[0].WordLevel.Find(w => w.Text == replaceWith).WordId);
            var word = documentDetails.Result.DocumentJson.Pages[0].WordLevel.Find(w => w.Text == replaceWith);
            var entityValue = word.Text + word.Space;

            entities.Add(new DocumentEntityTaggedReadDto
            {
                EntityId = new Guid(NameEntityId),
                WordIds = wordIds,
                Value = entityValue,
                TaggedAuthor = 0,
                DocumentId = documentId,
            });

            var updateDocumentDetailsRequest = new UpdateDocumentDetailsDto()
            {
                DocumentTypeId = documentDetails.Result.TaggedData.DocumentTypeLink[0].DocumentTypeId,
                DocumentTaggedDto = new DocumentTaggedDto()
                {
                    EntitiesTagged = entities,
                    IntentsTagged = intents,
                    DocumentTypeLink = documentDetails.Result.TaggedData.DocumentTypeLink
                }
            };

            await TagDocument(JsonConvert.SerializeObject(updateDocumentDetailsRequest), documentId);
            
            sw.Stop();
            Console.WriteLine($"process completed for document - {documentNo} - {sw.Elapsed.Seconds}");
            return documentId;
        }

        private static string CleanTextAndHtmlNewLineCharacters(string originalString)
        {
            originalString = originalString.Replace(IronPdfLicenseText, "");
            originalString = originalString.Replace("\r\n", "</br>");
            originalString = originalString.Replace("\n", "</br>");
            return $"{originalString}";
        }

        private async Task<bool> SendDocumentToBot(byte[] file, string fileNameWithExtension)
        {
            //string endpointUrl = "https://qa.simplifai.ai/da/externalapi/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
            //string apiKey = "OYGSRTENNWIHKWL";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);

                using var formData = new MultipartFormDataContent();
                using var fileContent = new StreamContent(new MemoryStream(file));

                formData.Add(fileContent, "files", fileNameWithExtension);
                var response = client.PostAsync(ExternalApiEndpointUrl, formData).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
                }

                return response.IsSuccessStatusCode;
            }
        }

        private async Task<DocumentSearchResponseDto> SearchForDocumentId(string requestBody)
        {
            string url = $"{DocBotApiBaseUrl}/{DocumentBotId}/Document/search";
            var searchResponse = new DocumentSearchResponseDto();

            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                searchResponse = JsonConvert.DeserializeObject<DocumentSearchResponseDto>(responseContent);
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error: " + responseContent);
            }
            return searchResponse;
        }

        private async Task<DocumentDetailsResponseDto> GetDocumentDetails(Guid documentId)
        {
            string url = $"{DocBotApiBaseUrl}/{DocumentBotId}/Document/{documentId}";

            var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
            var result = new DocumentDetailsResponseDto();

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<DocumentDetailsResponseDto>(responseContent);
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error: " + response.StatusCode);
            }
            return result;
        }

        private async Task TagDocument(string request, Guid documentId)
        {
            string url = $"{DocBotApiBaseUrl}/{DocumentBotId}/Document/{documentId}/details";

            using var requestBody = new StringContent(request);
            requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = _httpClient.PutAsync(url, requestBody).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
            }             
        }

        private async Task<bool> MarkDocumentsAsCompleted(IEnumerable<Guid> documentIds)
        {
            string url = $"{DocBotApiBaseUrl}/{DocumentBotId}/Document/status";

            var requestData = new
            {
                documentIds = documentIds,
                documentStatus = 2
            };
            var jsonRequest = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = _httpClient.PutAsync(url, content).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return false;
            }
        }   

        private string GenerateRandomString(int length, bool numeric)
        {
            const string alphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string numericChars = "0123456789";

            var chars = numeric ? numericChars : alphanumericChars;
            var random = new Random();
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, chars.Length);
                sb.Append(chars[index]);
            }

            return sb.ToString();
        }
    }
}
