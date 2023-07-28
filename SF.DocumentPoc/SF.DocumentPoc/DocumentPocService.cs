using Newtonsoft.Json;
using OfficeOpenXml;
using SF.DocumentPoc.Models;
using System.Collections.Concurrent;
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
        private string DocBotApiBaseUrl;
        private string DocumentBotId;
        private int DocumentCount = 0;
        private string ExternalApiEndpointUrl;
        private string ApiKey;
        private string ExcelFilePath;
        private string DocNamePrefix;

        public DocumentPocService(int envChoice, string accessToken, string documentbotId, int noOfDocuments, string externalApiEndpointUrl, string apiKey, string excelFilePath, string docNamePrefix)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("authorization", accessToken);
            DocumentBotId = documentbotId;
            DocumentCount = noOfDocuments;
            ExternalApiEndpointUrl = externalApiEndpointUrl;
            ApiKey = apiKey;
            ExcelFilePath = excelFilePath;
            DocNamePrefix = docNamePrefix;
            License.LicenseKey = "";
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
            var sw = Stopwatch.StartNew();
            Console.WriteLine("\n\nProcess Started !!!\n\n");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excelPackage = new ExcelPackage(new FileInfo(ExcelFilePath));
            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
            var entities = await GetDocumentbotEntities(DocumentBotId);

            for (int colIndex = 1; colIndex < worksheet.Dimension.Columns; colIndex++)
            {
                entities.Find(e => e.Name.ToLower() == worksheet.Cells[1, colIndex].Value?.ToString().ToLower())!.ExcelIndex = colIndex;
            }
            entities.RemoveAll(e => e.ExcelIndex == null || e.ExcelIndex == 0);

            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                using var PDFDocument = PdfDocument.FromFile(worksheet.Cells[row, worksheet.Dimension.Columns].Value?.ToString());
                string AllText = CleanTextAndHtmlNewLineCharacters(PDFDocument.ExtractAllText());
                
                var documents = new ConcurrentBag<NewDocument>();

                Console.WriteLine($"\n\n*************--- FOR TEMPLATE {row - 1} ---*************\n\n");
                Console.WriteLine("Document Creation Started");
                var sw1 = Stopwatch.StartNew();
                for (int i = 1; i <= DocumentCount; i++)
                {
                    var textReplacementList = new ConcurrentBag<TextReplacementHelperDto>();

                    for (int column = 1; column < worksheet.Dimension.Columns; column++)
                    {
                        textReplacementList.Add(new TextReplacementHelperDto
                        {
                            EntityId = entities.Find(e => e.ExcelIndex == column).Id,
                            OldText = worksheet.Cells[row, column].Value?.ToString(),
                            EntityType = entities.Find(e => e.ExcelIndex == column).Name
                        });
                    }

                    var doc = await GenerateDocument(textReplacementList, AllText, $"{DocNamePrefix}_Template_{row - 1}_Test_{i}.pdf");
                    documents.Add(doc);
                }
                sw1.Stop();
                Console.WriteLine($"\nTime taken to generate {DocumentCount} documents of template {row-1} - {sw1.Elapsed.Seconds} (In Sec)");

                Console.WriteLine($"\nSending documents of template - {row - 1} to bot and tagging\n");
                var sw4 = Stopwatch.StartNew();
                var documentIds = await SendToBotAndTag(documents);
                await MarkComplete(documentIds);
                sw4.Stop();
                Console.WriteLine($"\nTime taken to send & tag {DocumentCount} documents of template {row - 1} - {sw4.Elapsed.Minutes} (In Min)");

            }
            sw.Stop();
            Console.WriteLine("**************----- FINAL -----**************");
            Console.WriteLine($"\n\nTime taken to send {DocumentCount * (worksheet.Dimension.Rows - 1)} documents - {sw.Elapsed.Minutes} (In Min)\n\n");
        }

        private async Task MarkComplete(List<Guid> documentIds)
        {
            int batchSize = 200;
            int totalBatches = (int)Math.Ceiling((double)documentIds.Count/batchSize);
            for(int batchNumber = 0; batchNumber < totalBatches; batchNumber++)
            {
                var batchDocumentIds = documentIds.Skip(batchNumber * batchSize).Take(batchSize).ToList();
                var result = await MarkDocumentsAsCompleted(batchDocumentIds);
            }

        }

        public async Task<NewDocument> GenerateDocument(ConcurrentBag<TextReplacementHelperDto> textReplacementList, string documentText, string documentName)
        {
            foreach (var entity in textReplacementList)
            {
                if (entity.EntityType == "Designation")
                {
                    continue;
                }
                //entity.NewText = GenerateRandomString(entity.OldText);
                entity.NewText = GenerateRandomData(entity.EntityType);
                documentText = documentText.Replace(entity.OldText, entity.NewText);
            }

            var renderer = new ChromePdfRenderer();
            var pdf = renderer.RenderHtmlAsPdf(documentText);

            return new NewDocument()
            {
                BinaryData = pdf.BinaryData,
                FileName = documentName,
                TextReplacementList = textReplacementList
            };
        }

        private async Task<List<Guid>> SendToBotAndTag(ConcurrentBag<NewDocument> documents)
        {
            var documentIds = new List<Guid>();
            ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };
            Parallel.ForEach(documents, options, async (document, ct) =>
            {
                var botResponse = await SendDocumentToBot(document.BinaryData, document.FileName);
                var documentDetails = await GetSingleDocumentDetails(document.FileName);

                var entities = new List<DocumentEntityTaggedReadDto>();
                var intents = new List<DocumentIntentTaggedReadDto>();

                var intentWordIds = new List<int>() { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

                string intentValue = "";

                foreach (var wordId in intentWordIds)
                {
                    intentValue = intentValue + documentDetails.Result.DocumentJson.Pages[0].WordLevel[wordId].Text + documentDetails.Result.DocumentJson.Pages[0].WordLevel[wordId].Space;
                }

                intents.Add(new DocumentIntentTaggedReadDto()
                {
                    IntentId = new Guid("c18b3fa8-694e-40d7-baac-bb3ce3ef4035"),
                    WordIds = intentWordIds,
                    TaggedAuthor = 1,
                    DocumentId = documentDetails.Result.Id,
                    Value = intentValue
                });

                foreach (var item in document.TextReplacementList)
                {
                    var wordIds = new List<int>();

                    if (Enum.TryParse(item.EntityType, out RandomDataType dataType))
                    {
                        switch (dataType)
                        {
                            case RandomDataType.Name:
                                wordIds = new List<int>() { 5 };
                                break;

                            case RandomDataType.EmailId:
                                wordIds = new List<int>() { 73, 74, 75, 76, 77, 78, 79 };
                                break;

                            case RandomDataType.Address:
                                wordIds = new List<int>() { 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165 };
                                break;

                            case RandomDataType.Date:
                                wordIds = new List<int>() { 65, 66, 67, 68, 69 };
                                break;

                            case RandomDataType.UserId:
                                wordIds = new List<int>() { 83 };
                                break;

                            case RandomDataType.MobileNo:
                                wordIds = new List<int>() { 58, 59, 60 };
                                break;
                        }
                    }

                    //wordIds.Add(documentDetails.Result.DocumentJson.Pages[0].WordLevel.Find(w => w.Text == item.NewText).WordId);
                    //var word = documentDetails.Result.DocumentJson.Pages[0].WordLevel.Find(w => w.Text == item.NewText);

                    string entityValue = "";

                    foreach (var wordId in wordIds)
                    {
                        entityValue = entityValue + documentDetails.Result.DocumentJson.Pages[0].WordLevel[wordId].Text + documentDetails.Result.DocumentJson.Pages[0].WordLevel[wordId].Space;
                    }

                    entities.Add(new DocumentEntityTaggedReadDto
                    {
                        EntityId = item.EntityId,
                        WordIds = wordIds,
                        Value = entityValue,
                        TaggedAuthor = 1,
                        DocumentId = documentDetails.Result.Id,
                    });
                }

                entities.Add(new DocumentEntityTaggedReadDto()
                {
                    EntityId = new Guid("69e53a81-5407-480e-917d-68e33a85fd20"),
                    Value = "Data Engineer",
                    WordIds = new List<int>() { 169, 170 },
                    TaggedAuthor = 1,
                    DocumentId = documentDetails.Result.Id

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

                await TagDocument(JsonConvert.SerializeObject(updateDocumentDetailsRequest), documentDetails.Result.Id);
                documentIds.Add(documentDetails.Result.Id);
                Console.WriteLine($"Sent to bot - {document.FileName}");

            });
            return documentIds;
        }

        private async Task<DocumentDetailsResponseDto> GetSingleDocumentDetails(string documentName)
        {
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
            return await GetDocumentDetails(documentId);
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

        private async Task<List<EntityHelperDto>> GetDocumentbotEntities(string documentbotId)
        {
            string url = $"{DocBotApiBaseUrl}/{documentbotId}/entity/GetDocumentbotEntities?pageNumber=1&pageSize=100&name=&value=0&documentbotId={documentbotId}";
            var entityResponse = new EntityResponseDto();

            HttpResponseMessage response = _httpClient.GetAsync(url).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                entityResponse = JsonConvert.DeserializeObject<EntityResponseDto>(responseContent);
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
            }
            return entityResponse.Result.Records;
        }        

        private string GenerateRandomString(string inputString)
        {
            const string alphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const string alphabeticChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string numericChars = "0123456789";

            bool containsAlphabets = inputString.Any(char.IsLetter);
            bool containsNumbers = inputString.Any(char.IsDigit);

            if (containsAlphabets && !containsNumbers)
            {
                return GenerateRandomStringOfType(alphabeticChars, inputString.Length);
            }
            else if (!containsAlphabets && containsNumbers)
            {
                return GenerateRandomStringOfType(numericChars, inputString.Length);
            }
            else
            {
                return GenerateRandomStringOfType(alphanumericChars, inputString.Length);
            }
        }

        private string GenerateRandomStringOfType(string chars, int length)
        {
            var random = new Random();
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, chars.Length);
                sb.Append(chars[index]);
            }

            return sb.ToString();
        }

        private string GenerateRandomData(string randomDataType)
        {
            var _random = new Random();
            var nameOptions = new string[] { "Aarav", "Aanya", "Gagan", "Diya", "Aryan", "Mira", "Devan", "Elina", "Gauti", "Janvi",
                                            "Kunal", "Ishna", "Preet", "Trish", "Mohit", "Nehal", "Mukul", "Pooja", "Kiran", "Priya",
                                            "Rakes", "Shana", "Rahul", "Sakhi", "Alok", "Simmi", "Vikas", "Tanvi", "Sagar", "Tina",
                                            "Samir", "Tisha", "Amol", "Yammi", "Sunny", "Zara", "Siddh", "Anika", "Yuvan", "Ishaa",
                                            "Aashi", "Rishi", "Drishti", "Lucky", "Roopa", "Sahil", "Juhi", "Vishu", "Naina", "Aarti",
                                            "Rahil", "Mansi", "Shiva", "Anaya", "Rishi", "Pari", "Jaiya", "Nikku", "Samar", "Aisha",
                                            "Ankit", "Aaroh", "Faiza", "Jyoti", "Vedha", "Lisha", "Rohit", "Mithu", "Raman", "Ishra",
                                            "Manny", "Dinky", "Suman", "Swati", "Preet", "Anuva", "Rohan", "Hansa", "Pinky", "Misha",
                                            "Varun", "Yashi", "Saara", "Kavya", "Zaina", "Rishu", "Nihar", "Hemal", "Jatin", "Sange",
                                            "Shalu", "Vicky", "Bhavy", "Deepa", "Rahul", "Mansi", "Vinit", "Nidhi", "Ashna", "Vikku",
                                            "Aaran", "Aaren", "Aarez", "Aarman", "Aaron", "Aarron", "Aaryan", "Aaryn", "Aayan", "Aazaan", "Abaan", "Abbas"};

            var emailHelperOptions = new string[] { ".", "_" };

            var emailExt = new string[] { "@gmail.com", "@outlook.com", "@icloud.com", "@yahoo.com", "@simplifai.ai", "@hotmail.com",
                                          "@aol.com", "@protonmail.com", "@mail.com", "@zoho.com", "@yandex.com", "@fastmail.com",
                                          "@rediffmail.com", "@gmx.com", "@mailinator.com", "@inbox.com", "@rocketmail.com", "@yahoo.co.uk",
                                          "@outlook.in", "@live.com", "@yopmail.com", "@tutanota.com", "@mail.ru" };

            var prefixes = new string[] { "Evo", "Lunar", "Zephyr", "Chrono", "Sol", "Inferno", "Cryo", "Seren", "Vortex", "Aurora",
                                               "Blaze", "Volt", "Celestial", "Tempest", "Sapphire", "Radiant", "Dynamo", "Ember", "Thunder",
                                               "Galaxy", "Mystic", "Nebula", "Astral", "Haze", "Crimson", "Zodiac", "Orbit", "Luminous",
                                               "Eclipse", "Quasar", "Whirlwind", "Pulsar", "Spectral", "Enigma", "Frost", "Stellar", "Solar",
                                               "Lithium", "Plasma", "Aether", "Comet", "Electron", "Eternal", "Fusion", "Nuclear", "Synth",
                                               "Umbra", "Venom", "Zenith" };

            var buildingNames = new string[] { "Sky Tower", "City Center", "Liberty Plaza", "Tech Park", "Crystal Heights", "Summit Place",
                                               "Harmony Gardens", "Emerald View", "Innovation Hub", "Ocean View", "Horizon Square", "Sunrise Court",
                                               "Evergreen Estate", "Golden Gate", "Diamond Ridge", "Sunlight Gardens", "Lakeside Manor",
                                               "Crown Plaza", "Maple Grove", "Silver Oaks", "Riverfront Court", "Palm Paradise", "Bayside Residences",
                                               "Grand Boulevard", "Harbor Point", "Whispering Pines", "Vista Valley", "Meadowbrook Village",
                                               "Parkside Terrace", "Azure Heights", "Serenity Springs", "Marina Shores", "Mountain View", "Twin Oaks",
                                               "Majestic Gardens", "Birchwood Place", "Regal Ridge", "Sapphire Meadows", "Willowbrook Estate", "Tranquil Haven",
                                               "Amber Ridge", "Hillcrest Court", "Ocean Breeze", "Harvest Fields", "Royal Reserve", "Greenfield Heights", "Breezy Heights" };

            var areaNames = new string[] { "Bandra", "Juhu", "Koramangala", "Malad", "Connaught", "Andheri", "Saket", "Alipore", "Banjara", "Fort",
                                           "Chelsea", "Venice", "Copacabana", "Camden", "Montmartre", "Malabar", "Marais", "Pudong", "Darlinghurst",
                                           "Georgetown", "Wynwood", "Montecito", "Shinjuku", "Gracia", "Thamel", "Vesterbro", "Fitzroy", "Kreuzberg",
                                           "Shoreditch", "Södermalm", "Gastown", "Palermo", "Kiyosumi", "Frelard", "Grünerløkka", "Xuhui", "Cihangir",
                                           "Haga", "Kallio", "Miraflores", "Sheung", "Gion", "Kypseli", "Thonglor", "Kalamaja", "Salthill", "Greenpoint" };

            var cityNames = new string[] { "Delhi", "Mumbai", "Pune", "Salem", "Indore", "Agra", "Patna", "Ranchi", "Bhopal", "Surat",
                                           "Ajmer", "Latur", "Sagar", "Bikaner", "Kolar", "Hinda", "Adoni", "Baram", "Hosur", "Kapur",
                                           "Gonda", "Lanka", "Anand", "Sindh", "Hansi", "Rajah", "Ropar", "Nizam", "Mauni", "Deesa",
                                           "Mundi", "Chitt", "Basti", "Mandi", "Kadap", "Pilib", "Raiga", "Dharm", "Mursh", "Jhars",
                                           "Tiruv", "Bhadr", "Girid", "Nager", "Katra", "Kasau", "Sawai", "Hosur", "Bhadr", "Nawas",
                                           "Delhi", "Mumbai", "Pune", "Salem", "Indore", "Agra", "Patna", "Ranchi", "Bhopal", "Surat",
                                           "Ajmer", "Latur", "Sagar", "Bikaner", "Kolar", "Hinda", "Adoni", "Baram", "Hosur", "Kapur",
                                           "Gonda", "Lanka", "Anand", "Sindh", "Hansi", "Rajah", "Ropar", "Nizam", "Mauni", "Deesa",
                                           "Mundi", "Chitt", "Basti", "Mandi", "Kadap", "Pilib", "Raiga", "Dharm", "Mursh", "Jhars",
                                           "Tiruv", "Bhadr", "Girid", "Nager", "Katra", "Kasau", "Sawai", "Hosur", "Bhadr", "Nawas",
                                           "Hangzhou", "Kingston", "Valletta", "Salvador", "Surabaya", "Adelaide", "Hamilton", "Lausanne",
                                           "Brisbane", "Beijing", "Sofia", "Managua", "Belgrade", "Tbilisi", "Bogota", "Marrakech", "Marbella",
                                           "Florence", "Pretoria", "Canberra", "Helsinki", "Detroit", "Colombo", "Houston", "Nairobi", "Medellin",
                                           "Casablanca", "Hangzhou", "Curitiba", "Santiago", "NewYork","London","Tokyo","Paris","Mumbai",
                                           "Sydney","Shanghai","CapeTown","Rio","Barcelona","LosAngeles","HongKong","Berlin","Amsterdam",
                                           "Singapore","Rome","Toronto","Dubai","Seoul","Vienna","Stockholm","SanFrancisco","Prague",
                                           "Budapest","Bangkok","Copenhagen","Moscow","Dublin","Zurich","Wellington","Amman","Helsinki",
                                           "Reykjavik","Oslo","Jerusalem","KualaLumpur","Lisbon","Montreal","Warsaw","Athens","Nairobi",
                                           "Auckland","Cairo","BuenosAires","Delhi","Jakarta","Johannesburg","MexicoCity","Riyadh"};



            if (Enum.TryParse(randomDataType, out RandomDataType dataType))
            {
                switch (dataType)
                {
                    case RandomDataType.Name:
                        return nameOptions[_random.Next(0, nameOptions.Length)];

                    case RandomDataType.EmailId:
                        var emailId = $"{nameOptions[_random.Next(0, nameOptions.Length)].ToLower()}{emailHelperOptions[_random.Next(0, emailHelperOptions.Length)]}{prefixes[_random.Next(0, prefixes.Length)].ToLower()}{_random.Next(0, 999)}{emailExt[_random.Next(0, emailExt.Length)]}";
                        return emailId;

                    case RandomDataType.Address:
                        var address = $"{_random.Next(100, 999)}, {buildingNames[_random.Next(0, buildingNames.Length)]}, {areaNames[_random.Next(0, areaNames.Length)]}, {cityNames[_random.Next(0, cityNames.Length)]} - {_random.Next(100000, 999999)}.";
                        return address;

                    case RandomDataType.Date:

                        var randomDate = RandomDateTime(new DateTime(1990, 1, 1), DateTime.Now).AddDays(-1);
                        string[] dateFormats = new string[]
                        {
                            "dd/MM/yyyy",
                            "MM/dd/yyyy",
                            "yyyy-MM-dd",
                            "dd-MMM-yyyy",
                            "yyyy/MM/dd",
                            "dd-MMMM-yyyy",
                            "MMMM-dd, yyyy"
                        };
                        return randomDate.ToString(dateFormats[_random.Next(0, dateFormats.Length)]);

                    case RandomDataType.UserId:
                        var userId = $"{prefixes[_random.Next(0, prefixes.Length)]}{_random.Next(0, 1000)}";
                        return userId;

                    case RandomDataType.MobileNo:
                        var mobileNo = $"+{_random.Next(0, 99)} {GenerateRandomStringOfType("0123456789", 10)}";
                        return mobileNo;

                    default:
                        return "Unknown Data Type";
                }
            }
            else
            {
                return "Invalid Data Type";
            }
        }

        public static DateTime RandomDateTime(DateTime startDate, DateTime endDate)
        {
            Random random = new Random();
            int range = (endDate - startDate).Days;
            return startDate.AddDays(random.Next(range));
        }
    }

    public enum RandomDataType
    {
        Name = 1,
        EmailId = 2,
        Address = 3,
        Date = 4,
        UserId = 5,
        MobileNo = 6
    }
}
