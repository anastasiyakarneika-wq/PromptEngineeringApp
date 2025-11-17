namespace PromptEngineeringApp.Tests
{
    using Microsoft.Playwright;
    using System.Threading.Tasks;
    using Xunit;

    public class TodoTests
    {
        private readonly IPlaywright playwright;
        private readonly IBrowser browser;
        private readonly IBrowserContext context;

        public TodoTests()
        {
            playwright = Playwright.CreateAsync().Result;
            browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }).Result;
            context = browser.NewContextAsync().Result;
        }

        [Fact]
        public async Task AddValidTaskToTodoList()
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://todomvc.com/examples/react/");
            var todoPage = new TodoPage();

            // Add the task
            await todoPage.AddTask(page, "Buy groceries");

            // Assert the task exists
            Assert.True(await page.Locator($"internal:role=listitem >> internal:has-text='Buy groceries'").IsVisibleAsync(), "The task was not added correctly.");
        }

        [Fact]
        public async Task PreventAddingEmptyTask()
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://todomvc.com/examples/react/");
            var todoPage = new TodoPage();

            // Attempt to add an empty task
            await page.Keyboard.PressAsync("Enter");

            // Assert no tasks are added
            Assert.False(await page.Locator("internal:role=listitem").IsVisibleAsync(), "An empty task was added to the list.");
        }

        [Fact]
        public async Task PreventAddingWhitespaceOnlyTask()
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://todomvc.com/examples/react/");
            var todoPage = new TodoPage();

            // Attempt to add a whitespace-only task
            await page.FillAsync("internal:role=textbox[name=\"What needs to be done?\"i]", "    ");
            await page.Keyboard.PressAsync("Enter");

            // Assert no tasks are added
            Assert.False(await page.Locator("internal:role=listitem").IsVisibleAsync(), "A whitespace-only task was added to the list.");
        }

        [Fact]
        public async Task VerifyInputFieldIsClearedAfterAddingTask()
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://127.0.0.1:7001/");
            var todoPage = new TodoPage();

            // Add the task
            string taskName = "Call the doctor";
            await todoPage.AddTask(page, taskName);

            // Assert the input field is cleared
            string inputValue = await page.Locator("internal:role=textbox[name=\"What needs to be done?\"i]").InputValueAsync();
            Assert.Equal(string.Empty, inputValue);
        }

        public async Task Dispose()
        {
            await browser.DisposeAsync();
        }
    }
}