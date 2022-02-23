using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure;
using Azure.AI.TextAnalytics;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text;
using Microsoft.Extensions.Azure;
using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Data;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace SummarizeDocumentAzureFunction
{
    public static class SummarizeDocument
    {
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential("<INSERTKEY>");
        private static readonly Uri endpoint = new Uri("https://<INSERTDOMAIN>.cognitiveservices.azure.com/");
        private static string document;
        private static StringBuilder sb = new StringBuilder();


        [FunctionName("SummarizeDocument")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var formdata = await req.ReadFormAsync();
            var file = req.Form.Files["file"];
            var sExtractedText = await new StreamReader(req.Body).ReadToEndAsync();

            StringBuilder sbExtractedText = new StringBuilder();

            sb.Clear();

            var client = new TextAnalyticsClient(endpoint, credentials);
            document = sExtractedText.ToString();

            //Use this method if you want to parse through a PDF (not image PDF) document...MUCH faster.
            //PdfReader pdfReader = new PdfReader(file.OpenReadStream());
            //PdfDocument pdfDoc = new PdfDocument(pdfReader);
            
            //for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            //{
            //    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
            //    string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page),strategy);

            //    pageContent = pageContent.Replace("\r\n", " ");
            //    pageContent = pageContent.Replace("\n", " ");
            
            //    sbExtractedText.Append(pageContent);
            //}

            //if (sbExtractedText.Length > 0)
            //{
            //    document = sbExtractedText.ToString();
            //}
            //else
            //{
            //}
             
                
            
            await TextSummarizationExample(client);
                        
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(sb.ToString().Replace("\r\n", " "), Formatting.Indented);

            return (ActionResult)new OkObjectResult(JSONString);

        }
        static async Task TextSummarizationExample(TextAnalyticsClient client)
        {
            //string document = @"The extractive summarization feature uses natural language processing techniques to locate key sentences in an unstructured text document. 
            //    These sentences collectively convey the main idea of the document. This feature is provided as an API for developers. 
            //    They can use it to build intelligent solutions based on the relevant information extracted to support various use cases. 
            //    In the public preview,
            //    summarization supports several languages. It is based on pretrained multilingual transformer models, part of our quest for holistic representations. 
            //    It draws its strength from transfer learning across monolingual and harness the shared nature of languages to produce models of improved quality and efficiency.";




            // Prepare analyze operation input. You can add multiple documents to this list and perform the same
            // operation to all of them.
            var batchInput = new List<string>
            {
                document
            };

            TextAnalyticsActions actions = new TextAnalyticsActions()
            {
                ExtractSummaryActions = new List<ExtractSummaryAction>() {
                    new ExtractSummaryAction() {MaxSentenceCount=20 }
                    //new ExtractSummaryAction() {MaxSentenceCount=20, OrderBy=SummarySentencesOrder.Rank }
                    
                    
                }
            };


            // Start analysis process.
            AnalyzeActionsOperation operation = await client.StartAnalyzeActionsAsync(batchInput, actions);
            await operation.WaitForCompletionAsync();
            // View operation status.
            //Console.WriteLine($"AnalyzeActions operation has completed");
            //Console.WriteLine();

            //Console.WriteLine($"Created On   : {operation.CreatedOn}");
            //Console.WriteLine($"Expires On   : {operation.ExpiresOn}");
            //Console.WriteLine($"Id           : {operation.Id}");
            //Console.WriteLine($"Status       : {operation.Status}");

            //Console.WriteLine();
            // View operation results.

            await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
            {
                IReadOnlyCollection<ExtractSummaryActionResult> summaryResults = documentsInPage.ExtractSummaryResults;

                foreach (ExtractSummaryActionResult summaryActionResults in summaryResults)
                {
                    if (summaryActionResults.HasError)
                    {
                        Console.WriteLine($"  Error!");
                        Console.WriteLine($"  Action error code: {summaryActionResults.Error.ErrorCode}.");
                        Console.WriteLine($"  Message: {summaryActionResults.Error.Message}");
                        continue;
                    }

                    foreach (ExtractSummaryResult documentResults in summaryActionResults.DocumentsResults)
                    {
                        if (documentResults.HasError)
                        {
                            Console.WriteLine($"  Error!");
                            Console.WriteLine($"  Document error code: {documentResults.Error.ErrorCode}.");
                            Console.WriteLine($"  Message: {documentResults.Error.Message}");
                            continue;
                        }

                        Console.WriteLine($"  Extracted the following {documentResults.Sentences.Count} sentence(s):");
                        Console.WriteLine();

                        foreach (SummarySentence sentence in documentResults.Sentences)
                        {
                            //Console.WriteLine($"  Sentence: {sentence.Text}");
                            //Console.WriteLine();
                            sb.Append(sentence.Text);
                            sb.Append(Environment.NewLine);
                        }
                    }
                }

            }

            static async Task Main(string[] args)
            {
                var client = new TextAnalyticsClient(endpoint, credentials);
                await TextSummarizationExample(client);
            }
        }
    }
}
