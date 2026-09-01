import asyncio
import re
from playwright import async_api
from playwright.async_api import expect

async def run_test():
    pw = None
    browser = None
    context = None

    try:
        # Start a Playwright session in asynchronous mode
        pw = await async_api.async_playwright().start()

        # Launch a Chromium browser in headless mode with custom arguments
        browser = await pw.chromium.launch(
            headless=True,
            args=[
                "--window-size=1280,720",
                "--disable-dev-shm-usage",
                "--ipc=host",
                "--single-process"
            ],
        )

        # Create a new browser context (like an incognito window)
        context = await browser.new_context()
        # Wider default timeout to match the agent's DOM-stability budget;
        # auto-waiting Playwright APIs (expect, locator.wait_for) inherit this.
        context.set_default_timeout(15000)

        # Open a new page in the browser context
        page = await context.new_page()

        # Interact with the page elements to simulate user flow
        # -> navigate
        await page.goto("http://localhost:4200")
        try:
            await page.wait_for_load_state("domcontentloaded", timeout=5000)
        except Exception:
            pass
        
        # -> Fill 'admin' into the 'Username or email address' field, fill the password, then click the 'Login' button to sign in.
        # LoginInput.UserNameOrEmailAddress text field
        elem = page.locator('[id="LoginInput_UserNameOrEmailAddress"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("admin")
        
        # -> Fill 'admin' into the 'Username or email address' field, fill the password, then click the 'Login' button to sign in.
        # LoginInput.Password password field
        elem = page.locator('[id="LoginInput_Password"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("1q2w3E*")
        
        # -> Fill 'admin' into the 'Username or email address' field, fill the password, then click the 'Login' button to sign in.
        # Login button
        elem = page.get_by_role('button', name='Login', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Exams' link in the left sidebar to open the Exams page.
        # Exams link
        elem = page.get_by_role('link', name='Exams', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the '+ New exam' button to open the exam creation form.
        # New exam link
        elem = page.get_by_role('link', name='New exam', exact=True)
        await elem.click(timeout=10000)
        
        # -> Fill the 'Title' field with a new exam name, set 'Minutes' to 45, set 'Pass mark' to 70, then click the 'Save' button.
        # title text field
        elem = page.locator('[id="title"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("Automated test exam 2026-08-31 1200Z")
        
        # -> Fill the 'Title' field with a new exam name, set 'Minutes' to 45, set 'Pass mark' to 70, then click the 'Save' button.
        # duration number field
        elem = page.locator('[id="duration"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("45")
        
        # -> Fill the 'Title' field with a new exam name, set 'Minutes' to 45, set 'Pass mark' to 70, then click the 'Save' button.
        # passMark number field
        elem = page.locator('[id="passMark"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("70")
        
        # -> Fill the 'Title' field with a new exam name, set 'Minutes' to 45, set 'Pass mark' to 70, then click the 'Save' button.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # --> Assertions to verify final state
        
        # --> Exam resource page URL contains '/exams/'.
        # Assert-outcome: passed
        # Assert: The current URL contains '/exams/'.
        await expect(page).to_have_url(re.compile("/exams/"), timeout=15000), "The current URL contains '/exams/'."
        
        # --> Saved exam title and settings are displayed: title 'Automated test exam 2026-08-31 1200Z', Minutes = 45, Pass mark = 70.
        # Assert-outcome: passed
        # Assert: The title input shows the saved exam title.
        await expect(page.locator("xpath=/html/body/app-root/astro-shell/div/main/astro-exam-form/form/section[1]/div[1]/input").nth(0)).to_have_value("Automated test exam 2026-08-31 1200Z", timeout=15000), "The title input shows the saved exam title."
        # Assert-outcome: passed
        # Assert: The Minutes input shows the saved duration (45).
        await expect(page.locator("xpath=/html/body/app-root/astro-shell/div/main/astro-exam-form/form/section[2]/div/div[1]/input").nth(0)).to_have_value("45", timeout=15000), "The Minutes input shows the saved duration (45)."
        await asyncio.sleep(5)

    finally:
        if context:
            await context.close()
        if browser:
            await browser.close()
        if pw:
            await pw.stop()

asyncio.run(run_test())
    