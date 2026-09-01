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
        
        # -> Fill the 'Username or email address' field with 'admin', fill the 'Password' field with '1q2w3E*', then click the 'Login' button.
        # LoginInput.UserNameOrEmailAddress text field
        elem = page.locator('[id="LoginInput_UserNameOrEmailAddress"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("admin")
        
        # -> Fill the 'Username or email address' field with 'admin', fill the 'Password' field with '1q2w3E*', then click the 'Login' button.
        # LoginInput.Password password field
        elem = page.locator('[id="LoginInput_Password"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("1q2w3E*")
        
        # -> Fill the 'Username or email address' field with 'admin', fill the 'Password' field with '1q2w3E*', then click the 'Login' button.
        # Login button
        elem = page.get_by_role('button', name='Login', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Exams' link in the left sidebar to open the Exams list.
        # Exams link
        elem = page.get_by_role('link', name='Exams', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Questions' action for the exam titled 'امتحانٌ قيد الجلوس — للمراقبة' to open its Questions view.
        # Questions: امتحانٌ قيد الجلوس — للمراقبة link
        elem = page.get_by_role('link', name='Questions: امتحانٌ قيد الجلوس — للمراقبة', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the '+ Add question' button to open the question creation form.
        # Add question link
        elem = page.get_by_role('link', name='Add question', exact=True)
        await elem.click(timeout=10000)
        
        # -> Select the 'Single choice' question type from the 'What kind of question is this?' screen.
        # Single choice Marked automatically button
        elem = page.get_by_role('button', name='Single choice Marked automatically', exact=True)
        await elem.click(timeout=10000)
        
        # -> Fill the 'Question text' field with a valid question, add two option texts, and mark the correct option.
        # Question text
        elem = page.locator('[id="text"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("Test question: What is 2 + 2?")
        
        # -> Fill the 'Question text' field with a valid question, add two option texts, and mark the correct option.
        # Option text 1 text field
        elem = page.get_by_label('Option text 1', exact=True)
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("3")
        
        # -> Fill the 'Question text' field with a valid question, add two option texts, and mark the correct option.
        # Option text 2 text field
        elem = page.get_by_label('Option text 2', exact=True)
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("4")
        
        # -> Fill the 'Question text' field with a valid question, add two option texts, and mark the correct option.
        # Mark as correct radio button
        elem = page.locator('xpath=/html/body/app-root/astro-shell/div/main/astro-question-form/form/section[2]/astro-choice-editor/div/div[2]/label/input')
        await elem.click(timeout=10000)
        
        # -> Fill the 'Question text' field with a valid question, add two option texts, and mark the correct option.
        await page.mouse.wheel(0, 300)
        
        # -> Click the 'Save' button to save the question.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Save' button to submit the new question and return to the exam's question list so the new question can be verified.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Save' button to submit the new question and then verify the question appears in the exam's question list.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Save' button to submit the new question and then verify the question appears in the exam question list.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Open the 'Category' dropdown (labelled 'Not filed') so a category option can be selected.
        # Not filed Live English Live English Live English... dropdown
        elem = page.locator('[id="questionCategory"]')
        await elem.click(timeout=10000)
        
        # -> Select the 'مجال الفحص الآليّ' option from the Category dropdown to satisfy the required Category field.
        # Not filed Live English Live English Live English... dropdown
        elem = page.locator("xpath=/html/body/app-root/astro-shell/div/main/astro-question-form/form/section[3]/div/div/select").nth(0)
        await elem.wait_for(state="visible", timeout=10000)
        await elem.select_option("")
        
        # -> Click the 'Save' button to submit the new question and observe whether the editor closes and the question appears in the exam's question list.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Set the 'Time limit (seconds)' field to 5 and click the 'Save' button to submit the new question.
        # timer number field
        elem = page.locator('[id="timer"]')
        await elem.wait_for(state="visible", timeout=10000)
        await elem.fill("5")
        
        # -> Set the 'Time limit (seconds)' field to 5 and click the 'Save' button to submit the new question.
        # Save button
        elem = page.get_by_role('button', name='Save', exact=True)
        await elem.click(timeout=10000)
        
        # -> Click the 'Exams' link in the left sidebar to return to the exams list and then open the exam's Questions view to verify the new question 'Test question: What is 2 + 2?' is listed.
        # Exams link
        elem = page.get_by_role('link', name='Exams', exact=True)
        await elem.click(timeout=10000)
        
        # --> Assertions to verify final state
        
        # --> The exam's Questions view shows the new question 'Test question: What is 2 + 2?'.
        # Assert-outcome: passed
        # Assert: The exam question list contains the new question text.
        await expect(page.locator("xpath=/html/body/app-root/astro-shell/div/main").nth(0)).to_contain_text("Test question: What is 2 + 2?", timeout=15000), "The exam question list contains the new question text."
        await asyncio.sleep(5)

    finally:
        if context:
            await context.close()
        if browser:
            await browser.close()
        if pw:
            await pw.stop()

asyncio.run(run_test())
    