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
        
        # -> Navigate to the live exam link 'http://localhost:4200/exam/RaDU-PW60nGOjjXfXBYlRO1Xd9cdo6Anh361bwIa-v8' and verify the exam page and the 'ابدأ الامتحان' (Start Exam) button appear.
        await page.goto("http://localhost:4200/exam/RaDU-PW60nGOjjXfXBYlRO1Xd9cdo6Anh361bwIa-v8")
        try:
            await page.wait_for_load_state("domcontentloaded", timeout=5000)
        except Exception:
            pass
        
        # -> Click the 'Start the exam' (ابدأ الامتحان) button to begin the exam.
        # Start the exam button
        elem = page.get_by_role('button', name='Start the exam', exact=True)
        await elem.click(timeout=10000)
        
        # -> Select the first answer for 'ما عاصمة السعوديّة؟' by clicking the first option, then click the 'Next' button to go to Question 2.
        # الخيار الصحيح
        elem = page.locator('xpath=/html/body/app-root/astro-take-sitting/div/main/article/astro-choice-answer/fieldset/label')
        await elem.click(timeout=10000)
        
        # -> Select the first answer for 'ما عاصمة السعوديّة؟' by clicking the first option, then click the 'Next' button to go to Question 2.
        # Next button
        elem = page.get_by_role('button', name='Next', exact=True)
        await elem.click(timeout=10000)
        
        # -> Fill the 'اشرح بإيجاز سبب اختيارك.' answer box with a short response and click the 'Next' button to go to Question 3.
        # Your answer text area
        elem = page.get_by_label('Your answer', exact=True)
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("\u0644\u0623\u0646\u0647\u0627 \u0627\u0644\u0639\u0627\u0635\u0645\u0629 \u0648\u062a\u0636\u0645 \u0645\u0639\u0638\u0645 \u0627\u0644\u0645\u0624\u0633\u0633\u0627\u062a \u0627\u0644\u062d\u0643\u0648\u0645\u064a\u0629.")
        
        # -> Fill the 'اشرح بإيجاز سبب اختيارك.' answer box with a short response and click the 'Next' button to go to Question 3.
        # Next button
        elem = page.get_by_role('button', name='Next', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the visible 'Finish' button to submit the exam while Question 3 is left unanswered.
        # Finish button
        elem = page.get_by_role('button', name='Finish', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Keep going' button in the 'Finish and submit?' dialog to continue the exam and verify the dialog closes without submitting.
        # Keep going button
        elem = page.get_by_role('button', name='Keep going', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Finish' button to attempt submission and trigger the blank-answer warning dialog.
        # Finish button
        elem = page.get_by_role('button', name='Finish', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Keep going' button in the 'Finish and submit?' warning dialog to close the dialog and continue the exam.
        # Keep going button
        elem = page.get_by_role('button', name='Keep going', exact=True)
        await elem.click(timeout=10000)
        
        # --> Assertions to verify final state
        
        # --> The UI shows that Question 3 is unanswered (the question button is labeled 'Question 3 — no answer').
        # Assert-outcome: passed
        # Assert: Question 3 is marked as unanswered via its aria-label.
        await expect(page.locator("xpath=/html/body/app-root/astro-take-sitting/div/nav/ol/li[3]/button").nth(0)).to_have_attribute("aria-label", "Question 3 \u2014 no answer", timeout=15000), "Question 3 is marked as unanswered via its aria-label."
        
        # --> The candidate was returned to the exam and can continue because the Finish button is visible and the sitting URL is still loaded.
        await page.locator("xpath=/html/body/app-root/astro-take-sitting/div/footer/button[2]").nth(0).scroll_into_view_if_needed()
        # Assert-outcome: passed
        # Assert: The Finish button is visible so the candidate can continue the exam.
        await expect(page.locator("xpath=/html/body/app-root/astro-take-sitting/div/footer/button[2]").nth(0)).to_be_visible(timeout=15000), "The Finish button is visible so the candidate can continue the exam."
        # Assert-outcome: passed
        # Assert: The URL contains /sitting indicating the exam sitting page is still open.
        await expect(page).to_have_url(re.compile("/sitting"), timeout=15000), "The URL contains /sitting indicating the exam sitting page is still open."
        await asyncio.sleep(5)

    finally:
        if context:
            await context.close()
        if browser:
            await browser.close()
        if pw:
            await pw.stop()

asyncio.run(run_test())
    