# 📋 Project Requirements | متطلبات المشروع

## ✅ Functional Requirements | المتطلبات الوظيفية

- 📌 Candidate Registration  
  - Register candidate data (name, email, phone, position applied).
  - الربط بين المرشح والامتحان.

- 📌 Exam Management  
  - Create exams with time limit and specialization.
  - إدارة عدد المحاولات.

- 📌 Question Types  
  - Text, code output, multiple-choice, file uploads.
  - أنواع الأسئلة تشمل نص، كود، اختيار من متعدد، ملفات.

- 📌 Attempt Flow  
  - Track start/end time, auto-submit, prevent duplicates.
  - تتبع وقت المحاولة، إرسال تلقائي، منع التكرار.

- 📌 Grading System  
  - Smart grading for MCQs and code, manual for others.
  - تصحيح ذكي للأسئلة الموضوعية ويدوي للأسئلة المفتوحة.

- 📌 Exam Link Sharing  
  - Generate secure exam links with expiry and max attempts.
  - إنشاء روابط سرية تحتوي على صلاحية وتحديد عدد المحاولات.

- 📌 Dashboards  
  - Visual review dashboards for supervisors and HR.
  - لوحات تحكم للإحصائيات والنتائج ومراجعة الإجابات يدويًا.

---

## 🔐 Non-Functional Requirements | المتطلبات غير الوظيفية

- ✅ Secure API access (JWT / Role-based access).
  - حماية باستخدام JWT وصلاحيات حسب الدور.

- ✅ Multilingual Documentation
  - توثيق باللغتين العربية والإنجليزية.

- ✅ Scalable Architecture (DDD + ABP Modules)
  - هيكلية قابلة للتوسع تعتمد DDD وABP.

- ✅ Swagger Integration
  - تكامل كامل مع Swagger لتوثيق الـ API.

- ✅ Code Maintainability
  - Commented services and clear layering.

- ✅ File Upload & Cleanup
  - رفع ملفات آمن وتنظيف تلقائي للملفات القديمة.

---

## ⚠️ Constraints | القيود

- النظام لا يسمح بأكثر من محاولة واحدة إن تم تفعيل هذا الخيار في الرابط.
- الأسئلة البرمجية تعتمد على مقارنة `Output` فقط (ليس تنفيذ الكود فعليًا).

---


