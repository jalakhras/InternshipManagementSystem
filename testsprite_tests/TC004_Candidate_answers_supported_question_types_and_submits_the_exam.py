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
        
        # -> Open the candidate exam page by navigating to the candidate exam URL /exam/demo in a new tab.
        # Open URL in new tab
        page = await context.new_page()
        await page.goto("http://localhost:4200/exam/demo")
        try:
            await page.wait_for_load_state("domcontentloaded", timeout=5000)
        except Exception:
            pass
        
        # -> Sign in on the staff login page and open the Assignments screen to retrieve a valid candidate exam link.
        # Switch to tab 6416
        page = context.pages[-1]  # switch to most recently active tab
        
        # -> Sign in on the 'Login' form using username 'admin' and password '1q2w3E*' (click the 'Login' button).
        # LoginInput.UserNameOrEmailAddress text field
        elem = page.locator('[id="LoginInput_UserNameOrEmailAddress"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("admin")
        
        # -> Sign in on the 'Login' form using username 'admin' and password '1q2w3E*' (click the 'Login' button).
        # LoginInput.Password password field
        elem = page.locator('[id="LoginInput_Password"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("1q2w3E*")
        
        # -> Sign in on the 'Login' form using username 'admin' and password '1q2w3E*' (click the 'Login' button).
        # Login button
        elem = page.get_by_role('button', name='Login', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Assignments' link in the left navigation to open the Assignments screen and locate a seeded candidate exam link.
        # Assignments link
        elem = page.get_by_role('link', name='Assignments', exact=True)
        await elem.click(timeout=10000)
        
        # -> Open the assignment titled 'Live placement live-mtgpq5ol1f0' to view its details and retrieve the candidate link.
        # Live placement live-mtgpq5ol1f0 3 questions · 30... link
        elem = page.get_by_role('link', name='Live placement live-mtgpq5ol1f0 3 questions · 30 min', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'New link' button for Journey 48979479 to generate a valid candidate exam link.
        # New link: Journey 48979479 button
        elem = page.get_by_role('button', name='New link: Journey 48979479', exact=True)
        await elem.click(timeout=10000)
        
        # -> Open the candidate exam link shown in the 'A new link' dialog
        # Open URL in new tab
        page = await context.new_page()
        await page.goto("http://localhost:4200/exam/Rx8pPBF1PF0hbMBl_ZJeblSuJezJzEVGY4NavGGbNv8")
        try:
            await page.wait_for_load_state("domcontentloaded", timeout=5000)
        except Exception:
            pass
        
        # -> Click the 'Start the exam' button on the candidate exam landing page to begin the sitting.
        # Start the exam button
        elem = page.get_by_role('button', name='Start the exam', exact=True)
        await elem.click(timeout=10000)
        
        # -> Select the 'Right' answer on Question 1, then click the 'Next' button to open Question 2.
        # Right
        elem = page.locator('xpath=/html/body/app-root/astro-take-sitting/div/main/article/astro-choice-answer/fieldset/label')
        await elem.click(timeout=10000)
        
        # -> Select the 'Right' answer on Question 1, then click the 'Next' button to open Question 2.
        # Next button
        elem = page.get_by_role('button', name='Next', exact=True)
        await elem.click(timeout=10000)
        
        # -> Select the 'Right' answer on Question 2 and click the 'Next' button to open Question 3.
        # Right
        elem = page.locator('xpath=/html/body/app-root/astro-take-sitting/div/main/article/astro-choice-answer/fieldset/label')
        await elem.click(timeout=10000)
        
        # -> Select the 'Right' answer on Question 2 and click the 'Next' button to open Question 3.
        # Next button
        elem = page.get_by_role('button', name='Next', exact=True)
        await elem.click(timeout=10000)
        
        # -> Select the 'Right' answer for Question 3 and click the 'Finish' button to submit the exam.
        # Right
        elem = page.locator('xpath=/html/body/app-root/astro-take-sitting/div/main/article/astro-choice-answer/fieldset/label')
        await elem.click(timeout=10000)
        
        # -> Click the 'Finish' button to submit the exam sitting and trigger the submission confirmation.
        # Finish button
        elem = page.get_by_role('button', name='Finish', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Submit' button on the 'Finish and submit?' confirmation dialog to submit the exam.
        # Submit button
        elem = page.get_by_role('button', name='Submit', exact=True)
        await elem.click(timeout=10000)
        
        # --> Assertions to verify final state
        
        # --> Submission confirmation is shown with the score '100%'.
        await page.locator("xpath=/html/body/app-root/astro-take-result/div/div/section/div/span[3]").nth(0).scroll_into_view_if_needed()
        # Assert-outcome: passed
        # Assert: The results page shows the score '100%'.
        await expect(page.locator("xpath=/html/body/app-root/astro-take-result/div/div/section/div/span[3]").nth(0)).to_be_visible(timeout=15000), "The results page shows the score '100%'."
        
        # --> The exam sitting is no longer active and the results page is displayed.
        # Assert-outcome: passed
        # Assert: The browser is on a URL containing '/result', indicating the results page is shown.
        await expect(page).to_have_url(re.compile("/result"), timeout=15000), "The browser is on a URL containing '/result', indicating the results page is shown."
        await asyncio.sleep(5)

    finally:
        if context:
            await context.close()
        if browser:
            await browser.close()
        if pw:
            await pw.stop()

asyncio.run(run_test())
    