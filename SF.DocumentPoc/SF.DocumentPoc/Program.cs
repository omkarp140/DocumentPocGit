// See https://aka.ms/new-console-template for more information
using SF.DocumentPoc;

Console.WriteLine("Hello!!!");

//Console.WriteLine("\n\nSelect The Envrionment : ");
////Console.WriteLine("Press 1 for - DEV");
////Console.WriteLine("Press 2 for - QA");
////Console.WriteLine("Press 3 for - STAGING");
//Console.WriteLine("For DEV Environment     - Press 1");
//Console.WriteLine("For QA Environment      - Press 2");
//Console.WriteLine("For STAGING Environment - Press 3");
//Console.WriteLine("\nEnter your choice : ");
//var envChoice = Console.ReadLine();

//Console.WriteLine("\n\nEnter Documentbot Id : ");
//var documentbotId = Console.ReadLine();

//Console.WriteLine("\n\nEnter External API Endpoint For the same bot : ");
//var externalApiEndpoint = Console.ReadLine();

//Console.WriteLine("\n\nEnter Api Key : ");
//var apiKey = Console.ReadLine();

//Console.WriteLine("\n\nEnter Access Token : ");
//var accessToken = Console.ReadLine();

//Console.WriteLine("\n\nEnter File Path For Excel : ");
//var excelFilePath = Console.ReadLine();

//Console.WriteLine("\n\nEnter no of documents to send to the bot : ");
//var noOfDocuments = Console.ReadLine();

//var test = new DocumentPocService(int.Parse(envChoice), accessToken, documentbotId, int.Parse(noOfDocuments), externalApiEndpoint, apiKey, excelFilePath);
//test.ReadFromExcel(); 

var accessToken = "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjhENDZFMDk4OUZERUQwODA5NTYyNzFBRTQ0NTgyQTg3NkYwMkE3OTAiLCJ0eXAiOiJhdCtqd3QiLCJ4NXQiOiJqVWJnbUpfZTBJQ1ZZbkd1UkZncWgyOENwNUEifQ.eyJuYmYiOjE2ODk4NTgwODUsImV4cCI6MTY4OTg5NDA4NSwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS1xYS5zaW1wbGlmYWkuYWkiLCJhdWQiOiJzZi1kYS1hZG1pbi1hcGkiLCJjbGllbnRfaWQiOiJqcy5zZi5kYSIsInN1YiI6Im9ta2FyLnBhdGlsa2FyYWRlQHNpbXBsaWZhaS5haSIsImF1dGhfdGltZSI6MTY4OTg1ODA4NCwiaWRwIjoibG9jYWwiLCJuYW1lIjoib21rYXIucGF0aWxrYXJhZGVAc2ltcGxpZmFpLmFpIiwidXNlcklkIjoiNWM1MjU4ODAtZTNjMi00MTI3LThkYjItYzM2MTdhMjM1MWRiIiwiY3VzdG9tZXJJZCI6IjEwNDY4YjNlLWVhMDItNGY5NS04MjczLTQ3OWFlM2E1OGQ4NSIsInNjb3BlIjpbIm9wZW5pZCIsInByb2ZpbGUiLCJzZi1kYS1hZG1pbi1hcGkiXSwiYW1yIjpbInB3ZCJdfQ.hXFMlDciJLwNFDQenE-eNP2ysog4R5TzSP_8wUt2I7PgrGpeJGY5pAGE2EQifXYfFuqMiueSpJF0ddIT8Nv4k5ZmkHM2CUfcWM2TAjV-Va1YTt277BWfJqRojZbwiKwglafSQlOvduOc85KuQDSMhqCTA4bRWfHEdJhMoKEM6-4KNn0MZb1W497FwyvbLpvxQHiuI1-Id3ixETwhtJENwwqnl3ursRJpKrzprFQHNkBZRJ4EEnBeed8_psNy9Qmf_-bcKxLnhfuH96H_B0EHwiXR7pMAwSZf7sehz4np1qqCtMsM2dZz9UkBFR4T170TgxErSqByuGRWRorDb87Uow";
var documentbotId = "870c838e-e450-4d79-8d97-2d89bb3ee237";
var externalApiEndpoint = "https://qa.simplifai.ai/da/externalapi/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
var apiKey = "OYGSRTENNWIHKWL";
var excelFilePath = "D:\\Others\\SF-Test\\DocumentPocGit\\Documents\\MultiEntity.xlsx";

var test = new DocumentPocService(2, accessToken, documentbotId, 2, externalApiEndpoint, apiKey, excelFilePath);
test.ReadFromExcel(); 