using IronOcr;
using Newtonsoft.Json;
using SF.DocumentPoc.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace SF.DocumentPoc
{
    public class DocumentPocService
    {
        private static string ironPdfLicenseText = "To extract all of the page text, obtain a license key from https://ironpdf.com/licensing/";
        private readonly string AccessToken = "Insert Access Token For Respective Environment";
        public async Task GenerateDocAndSendToBot()
        {
            var sw = new Stopwatch();
            sw.Start();
            var filePath = @"D:\\Others\\SF-Test\\DocumentPocGit\\Documents\\PocDoc.pdf";

            using PdfDocument PDF = PdfDocument.FromFile(filePath);
            // Get all text to put in a search index
            string AllText = CleanTextAndHtmlNewLineCharacters(PDF.ExtractAllText());

            AllText = AllText.Replace("Omkar", "ABCD");
            var renderer = new ChromePdfRenderer();

            // Create a PDF from a HTML string using C#
            var pdf = renderer.RenderHtmlAsPdf(AllText);

            var documentName = "Test4.pdf";
            var botResponse = await SendDocumentToBot(pdf.BinaryData, documentName);

            IronTesseract ocr = new IronTesseract();
            OcrInput input = new OcrInput();
            input.AddPdf(filePath);
            var res = ocr.Read(input);

            //var wordId = res.Words.Where(w => w.Text == "Omkar").Select(w => w.WordNumber).ToList();
            var wordIndexes = res.Words.Select((w, index) => w.Text == "Omkar" ? index : -1).Where(index => index != -1).ToList();

            var documentSearchRequestDto = new GetDocumentRequestDto();
            documentSearchRequestDto.SearchText = documentName;
            documentSearchRequestDto.StartDate = DateTime.ParseExact("1999-12-31T18:30:00.000Z", "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            documentSearchRequestDto.EndDate = DateTime.UtcNow;
            documentSearchRequestDto.PageNumber = 1;
            documentSearchRequestDto.PageSize = 10;
            var searchResult = await SearchForDocumentId(JsonConvert.SerializeObject(documentSearchRequestDto));
            var documentId = searchResult.Result.Records.First().Id;
            var documentDetails = await GetDocumentDetails(documentId);

            var entities = new List<DocumentEntityTaggedReadDto>();
            var intents = new List<DocumentIntentTaggedReadDto>();
            var documentTypeLink = new List<DocumentTypeLinkReadDto>();

            var wordIds = new List<int>();
            foreach(var index in wordIndexes)
            {
                wordIds.Add(index);
                var word = documentDetails.Result.DocumentJson.Pages[0].WordLevel.FirstOrDefault(w => w.WordId == index);
                var entityValue = word.Text + word.Space;

                entities.Add(new DocumentEntityTaggedReadDto
                {
                    EntityId = new Guid("fba3155d-01cd-4698-a6d9-d4dc6640adce"),
                    WordIds = wordIds,
                    //WordIds = JsonConvert.SerializeObject(wordIds.ToArray()),
                    Value = entityValue,
                    TaggedAuthor = 0,
                    DocumentId = documentId,
                });
            }

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
            Console.WriteLine($"done - {sw.Elapsed.Seconds}");

        }

        private static string CleanTextAndHtmlNewLineCharacters(string originalString)
        {
            originalString = originalString.Replace(ironPdfLicenseText, "");
            originalString = originalString.Replace("\r\n", "</br>");
            originalString = originalString.Replace("\n", "</br>");
            return $"{originalString}";
        }

        private async Task<bool> SendDocumentToBot(byte[] file, string fileNameWithExtension)
        {
            string endpointUrl = "https://qa.simplifai.ai/da/externalapi/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
            string apiKey = "OYGSRTENNWIHKWL";

            //string endpointUrl = "http://localhost:5034/documentbot/0bd44a51-97d1-4618-8e8c-4cda3688a1ad/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
            //string apiKey = "XMPYVDFCKNBIPVS";

            using (HttpClient client = new HttpClient())
            {
                // Set the x-api-key header
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                // Create a multipart form data content
                MultipartFormDataContent formData = new MultipartFormDataContent();

                // Create a stream content for the file
                StreamContent fileContent = new StreamContent(new MemoryStream(file));

                // Add the file content to the form data
                formData.Add(fileContent, "files", fileNameWithExtension);
                HttpResponseMessage response = client.PostAsync(endpointUrl, formData).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    //var responseFromBot = JsonConvert.DeserializeObject<List<BotHandlingData>>(responseContent);
                    //Console.WriteLine("Response: " + responseContent);
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
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "https://qa.simplifai.ai/da/api/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/Document/search";
                    //string url = "http://localhost:5010/documentbot/0bd44a51-97d1-4618-8e8c-4cda3688a1ad/Document/search";

                    var searchResponse = new DocumentSearchResponseDto();

                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("authorization", AccessToken);

                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = client.PostAsync(url, content).GetAwaiter().GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        searchResponse = JsonConvert.DeserializeObject<DocumentSearchResponseDto>(responseContent);                        
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                    return searchResponse;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task<DocumentDetailsResponseDto> GetDocumentDetails(Guid documentId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://qa.simplifai.ai/da/api/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/Document/{documentId}";

                var searchResponse = new DocumentSearchResponseDto();

                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("authorization", AccessToken);

                HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
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
        }

        private async Task TagDocument(string request, Guid documentId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://qa.simplifai.ai/da/api/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/Document/{documentId}/details";

                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("authorization", AccessToken);

                var requestBody = new StringContent(request);
                requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = client.PutAsync(url, requestBody).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }


        }
    }
}
