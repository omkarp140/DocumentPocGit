﻿// See https://aka.ms/new-console-template for more information
using SF.DocumentPoc;

Console.WriteLine("Hello!!!");

//Console.WriteLine("\n\nSelect The Envrionment : ");
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

//Console.WriteLine("\n\nEnter prefix for document name :");
//Console.WriteLine("Note : Please provide prefix which is not already present in existing documents in the bot");
//Console.WriteLine("Example : If your provide 'DocTest' as prefix the documents will be named as 'DocTest_Template_1_Test_1.pdf, DocTest_Template_1_Test_2.pdf, DocTest_Template_2_Test_1.pdf, DocTest_Template_2_Test_2.pdf' and so on...");
//Console.WriteLine("\nEnter document name prefix :");
//var docNamePrefix = Console.ReadLine();

//var test = new DocumentPocService(int.Parse(envChoice), accessToken, documentbotId, int.Parse(noOfDocuments), externalApiEndpoint, apiKey, excelFilePath, docNamePrefix);
//test.ReadFromExcel();


var accessToken = "";
var documentbotId = "870c838e-e450-4d79-8d97-2d89bb3ee237";
var externalApiEndpoint = "https://qa.simplifai.ai/da/externalapi/documentbot/870c838e-e450-4d79-8d97-2d89bb3ee237/ExternalDocumentProcessing/FromFiles?CustomerId=10468b3e-ea02-4f95-8273-479ae3a58d85";
var apiKey = "";
var excelFilePath = "D:\\Others\\SF-Test\\DocumentPocGit\\Documents\\MultiEntity.xlsx";

var test = new DocumentPocService(2, accessToken, documentbotId, 25, externalApiEndpoint, apiKey, excelFilePath, "Summer3");
test.ReadFromExcel();