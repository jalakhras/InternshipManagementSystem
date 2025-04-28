# 📌 Use Cases | حالات الاستخدام


---

## 🎯 Use Case 1: HR Manager assigns exams to candidates
**Actors:** HR Manager, Candidate  
**Preconditions:** Candidate is registered in the system.  
**Description:** The HR Manager selects an exam and assigns it to one or more candidates.  
**Flow:**
1. HR Manager logs in.
2. Navigates to Candidates.
3. Selects a candidate and assigns an exam.
4. System sends a secure exam link.

---

## 🎯 Use Case 2: Candidate accesses and submits exam
**Actors:** Candidate  
**Preconditions:** Candidate received a valid secure exam link.  
**Description:** The candidate accesses the exam via the token, completes it, and submits answers.  
**Flow:**
1. Candidate opens the exam link.
2. Fills in answers (text/code/upload).
3. Submits the exam.
4. System triggers auto/manual grading.

---

## 🎯 Use Case 3: Supervisor creates a training plan for trainees
**Actors:** Supervisor  
**Description:** Supervisor creates a training plan and links it to a trainee group.  
**Flow:**
1. Supervisor logs in.
2. Navigates to "Training Plans".
3. Creates a plan with schedule, goals, and resources.
4. Assigns the plan to trainees.

---

## 🎯 Use Case 4: Admin manages system users and roles
**Actors:** Admin  
**Description:** Admin configures user roles and access permissions.  
**Flow:**
1. Admin logs in.
2. Opens Users & Permissions.
3. Creates or updates users.
4. Assigns roles (HR, Supervisor, Trainee, etc.).

---

## 🎯 Use Case 5: Candidate exam is reviewed manually
**Actors:** Reviewer (Supervisor or HR)  
**Description:** Reviewer accesses dashboard to manually grade answers that require human evaluation.  
**Flow:**
1. Reviewer logs in.
2. Navigates to "Manual Review".
3. Filters by pending attempts.
4. Reviews and scores open-ended answers.

---

## 🎯 Use Case 6: Trainer creates questions and exams
**Actors:** Supervisor / Educator  
**Description:** System users can create new exams and populate them with multimedia and logical questions.  
**Flow:**
1. User logs in.
2. Navigates to “Exam Management”.
3. Adds new exam and attaches questions (MCQ, text, code, etc.).

---

## 🎯 Use Case 7: Admin reviews exam result dashboards
**Actors:** Admin  
**Description:** Admin monitors exam performance statistics per candidate, exam, or department.  
**Flow:**
1. Admin logs in.
2. Opens “Results Dashboard”.
3. Filters by candidate or exam.
4. Views summaries and trends.

---

## 🎯 Use Case 8: Self-registration for trainees (if enabled)
**Actors:** Trainee  
**Description:** Trainee registers directly using a public form if the feature is allowed.  
**Flow:**
1. Trainee opens registration form.
2. Fills in personal and professional details.
3. Account created and approval awaited (optional).

---

## 🎯 Use Case 9: Training center uses platform to evaluate students
**Actors:** Center Supervisor / Admin  
**Description:** Education centers onboard students and assess them via exams.  
**Flow:**
1. Admin creates student accounts.
2. Assigns exams to each student.
3. Tracks submissions and generates reports.

---

## 🎯 Use Case 10: Technical department trains employees using the platform
**Actors:** Internal Trainer / IT Admin  
**Description:** Internal employees undergo structured training programs and exams via the platform.  
**Flow:**
1. Employees added as users.
2. Assigned training modules and exams.
3. Performance tracked via dashboard.