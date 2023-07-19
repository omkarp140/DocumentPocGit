using Newtonsoft.Json;
using OfficeOpenXml;
using SF.DocumentPoc.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;

namespace SF.DocumentPoc
{
    public class DocumentPocService
    {
        private static string IronPdfLicenseText = "To extract all of the page text, obtain a license key from https://ironpdf.com/licensing/";
        private readonly string AccessToken = "AccessToken";

        private readonly HttpClient _httpClient;
        private readonly string QaDocBotApiBaseUrl = "https://qa.simplifai.ai/da/api/documentbot";
        private readonly string DocumentBotId = "870c838e-e450-4d79-8d97-2d89bb3ee237";
        private readonly string NameEntityId = "fba3155d-01cd-4698-a6d9-d4dc6640adce";
        private readonly int DocumentCount = 5;
        string TemplateFilepath = "";

        public DocumentPocService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("authorization", AccessToken);
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
                        documentIds = new List<Guid>();
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
            var sw = new Stopwatch();
            sw.Start();

            using PdfDocument PDF = PdfDocument.FromFile(TemplateFilepath);
            string AllText = CleanTextAndHtmlNewLineCharacters(PDF.ExtractAllText());

            string replaceWith = GenerateRandomString(textToReplace.Length, false);
            AllText = AllText.Replace(textToReplace, replaceWith);
            var renderer = new ChromePdfRenderer();
            var pdf = renderer.RenderHtmlAsPdf(AllText);

            var documentName = $"FourthTestRun_{documentNo}.pdf";
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
            var documentId = searchResult.Result.Records.First().Id;
            var documentDetails = await GetDocumentDetails(documentId);

            var entities = new List<DocumentEntityTaggedReadDto>();
            var intents = new List<DocumentIntentTaggedReadDto>();

            var wordIds = new List<int>();

            wordIds.Add(documentDetails.Result.DocumentJson.Pages[0].WordLevel.FirstOrDefault(w => w.Text == replaceWith).WordId);
            var word = documentDetails.Result.DocumentJson.Pages[0].WordLevel.FirstOrDefault(w => w.Text == replaceWith);
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
            string endpointUrl = "https://qa.simplifai.ai/da/externalapi/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
            string apiKey = "OYGSRTENNWIHKWL";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                MultipartFormDataContent formData = new MultipartFormDataContent();
                StreamContent fileContent = new StreamContent(new MemoryStream(file));

                formData.Add(fileContent, "files", fileNameWithExtension);
                HttpResponseMessage response = client.PostAsync(endpointUrl, formData).GetAwaiter().GetResult();

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
            string url = $"{QaDocBotApiBaseUrl}/{DocumentBotId}/Document/search";
            var searchResponse = new DocumentSearchResponseDto();

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();

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
            string url = $"{QaDocBotApiBaseUrl}/{DocumentBotId}/Document/{documentId}";

            HttpResponseMessage response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
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
            string url = $"{QaDocBotApiBaseUrl}/{DocumentBotId}/Document/{documentId}/details";

            var requestBody = new StringContent(request);
            requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = _httpClient.PutAsync(url, requestBody).GetAwaiter().GetResult();

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
            string url = $"{QaDocBotApiBaseUrl}/{DocumentBotId}/Document/status";

            var requestData = new
            {
                documentIds = documentIds,
                documentStatus = 2
            };
            var jsonRequest = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = _httpClient.PutAsync(url, content).GetAwaiter().GetResult();

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
