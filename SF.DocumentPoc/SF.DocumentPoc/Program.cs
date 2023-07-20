// See https://aka.ms/new-console-template for more information
using SF.DocumentPoc;

Console.WriteLine("Hello!!!");

Console.WriteLine("\n\nSelect The Envrionment : ");
//Console.WriteLine("Press 1 for - DEV");
//Console.WriteLine("Press 2 for - QA");
//Console.WriteLine("Press 3 for - STAGING");
Console.WriteLine("For DEV Environment     - Press 1");
Console.WriteLine("For QA Environment      - Press 2");
Console.WriteLine("For STAGING Environment - Press 3");
Console.WriteLine("\nEnter your choice : ");
var envChoice = Console.ReadLine();

Console.WriteLine("\n\nEnter Documentbot Id : ");
var documentbotId = Console.ReadLine();

Console.WriteLine("\n\nEnter External API Endpoint For the same bot : ");
var externalApiEndpoint = Console.ReadLine();

Console.WriteLine("\n\nEnter Api Key : ");
var apiKey = Console.ReadLine();

Console.WriteLine("\n\nEnter Access Token : ");
var accessToken = Console.ReadLine();

Console.WriteLine("\n\nEnter File Path For Excel : ");
var excelFilePath = Console.ReadLine();

Console.WriteLine("\n\nEnter no of documents to send to the bot : ");
var noOfDocuments = Console.ReadLine();

var test = new DocumentPocService(int.Parse(envChoice), accessToken, documentbotId, int.Parse(noOfDocuments), externalApiEndpoint, apiKey, excelFilePath);
//test.GenerateDocAndSendToBot(); 
test.ReadFromExcel(); 