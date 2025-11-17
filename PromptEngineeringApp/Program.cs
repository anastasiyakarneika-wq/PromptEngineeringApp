using PromptEngineeringApp.Generators;


//Console.WriteLine("enter file for generating test cases");
//var fileWithReqs = Console.ReadLine();
//await new TestCaseGenerator()
//    .GenerateTestCasesAsync
//    ($"requirements/{fileWithReqs}",
//    "testcases/addtask.json");
//Console.WriteLine("Generation of page object");
//await new PageObjectGenerator().GeneratePageObjectAsync("traces/todo_mvc.traces",
//    "../../../TodoPage.cs");

await new TestAutomationTool().AutomateTestsAsync("testcases/addtask.json", "../../../TodoPage.cs", "../../../Test.cs");