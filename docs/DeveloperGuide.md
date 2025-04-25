# 🧑‍💻 Developer Guide | دليل المطور

## 🔧 Setup Instructions | تعليمات الإعداد

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/internship-management-system.git
   cd internship-management-system
   ```

2. **Configure the database**
   - Update the connection string in `appsettings.json` under the `ConnectionStrings` section.
   ```json
   "ConnectionStrings": {
     "Default": "Server=.;Database=InternshipDB;Trusted_Connection=True;"
   }
   ```

3. **Run migrations**
   ```bash
   dotnet ef database update --project src/InternshipManagementSystem.EntityFrameworkCore
   ```

4. **Run the application**
   ```bash
   cd src/InternshipManagementSystem.HttpApi.Host
   dotnet run
   ```

5. **Access Swagger UI**
   - Navigate to: `http://localhost:<PORT>/swagger`

---

## 🧱 Project Structure | هيكلية المشروع

- `Domain`: يحتوي على الكيانات والمنطق الأساسي.
- `Application`: يحتوي على AppServices والمنطق التطبيقي.
- `Application.Contracts`: يحتوي على DTOs والعقود.
- `HttpApi`: واجهات REST Controllers.
- `EntityFrameworkCore`: إعداد EF Core والتهيئة وقاعدة البيانات.

---

## 🧪 Testing Strategy | استراتيجية الاختبار

- يعتمد على **xUnit** مع **AbpTestBase**.
- يغطي AppServices والخدمات الداخلية.
- يتم وضع الاختبارات ضمن مجلد `test/`.

---

## 📁 Folder Structure | هيكل المجلدات المقترح

```
docs/               # ملفات التوثيق
src/                # كود المشروع الأساسي
  ├── .Application
  ├── .Application.Contracts
  ├── .Domain
  ├── .EntityFrameworkCore
  ├── .HttpApi
  └── .HttpApi.Host
test/               # اختبارات الوحدة والتكامل
```

---

## 🛡️ Roles & Permissions | الأدوار والصلاحيات

- النظام يدعم صلاحيات مرنة باستخدام `PermissionDefinitionProvider`.
- الأدوار المضمنة:
  - Admin
  - Supervisor
  - HR Manager
  - Candidate

---

## 🧩 Modules | الوحدات البرمجية

- **TrainingManagement**: إدارة الامتحانات والأسئلة والمحاولات.
- **CandidateManagement**: عمليات تخص المرشحين.
- **FileManagement**: إدارة ورفع الملفات.
- **Settings**: إعدادات التسجيل الذاتي والخصائص الديناميكية.
