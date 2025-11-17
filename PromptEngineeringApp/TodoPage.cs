using Microsoft.Playwright;

namespace PromptEngineeringApp
{
    public class TodoPage
    {
        private const string inputTextbox = "internal:role=textbox[name=\"What needs to be done?\"i]";
        private const string activeLink = "internal:role=link[name=\"Active\"i]";
        private const string completedLink = "internal:role=link[name=\"Completed\"i]";
        private const string allLink = "internal:role=link[name=\"All\"i]";
        private const string clearCompletedButton = "internal:role=button[name=\"Clear completed\"i]";
        private const string closeButton = "internal:role=button[name=\"×\"i]";

        public async Task AddTask(IPage page, string task)
        {
            await page.FillAsync(inputTextbox, task);
            await page.Keyboard.PressAsync("Enter");
        }

        public async Task CheckTask(IPage page, string taskName)
        {
            var checkboxLocator = $"internal:role=listitem >> internal:has-text=\"{taskName}\"i >> internal:role=checkbox";
            await page.CheckAsync(checkboxLocator);
        }

        public async Task ClickActive(IPage page)
        {
            await page.ClickAsync(activeLink);
        }

        public async Task ClickCompleted(IPage page)
        {
            await page.ClickAsync(completedLink);
        }

        public async Task ClickAll(IPage page)
        {
            await page.ClickAsync(allLink);
        }

        public async Task ClearCompletedTasks(IPage page)
        {
            await page.ClickAsync(clearCompletedButton);
        }

        public async Task DeleteTask(IPage page)
        {
            await page.ClickAsync(closeButton);
        }
    }
}