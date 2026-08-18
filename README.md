# 🛒 نظام نقاط البيع وإدارة السوبرماركت (Supermarket POS System) - Backend API

مشروع خادم API عالي الأداء والإنتاجية لبناء وإدارة السوبرماركت ونقاط البيع (POS). مبني بأحدث تقنيات **.NET 10** ونمط **CQRS** لتنفيذ جميع العمليات المالية والمخزنية بأمان وسرعة عالية.

---

## 🛠️ التقنيات المستخدمة (Tech Stack)

- **الإطار البرمجي**: ASP.NET Core 10 (Web API)
- **نمط التصميم**: CQRS Pattern باستخدام مكتبة **MediatR**
- **وصول البيانات**: Dapper (Micro-ORM) لأفضل أداء في استعلامات SQL
- **قاعدة البيانات**: MySQL / MariaDB
- **التشفير والحماية**: 
  - BCrypt.NET لتشفير كلمات المرور
  - JWT (JSON Web Tokens) لإدارة الجلسات ووسوم الصلاحيات (Claims)
- **التحقق من البيانات**: FluentValidation
- **التوثيق وتطهير النصوص**: WebUtility.HtmlEncode لتأمين مخرجات الفواتير والطباعة

---

## ✨ المميزات الرئيسية (Key Features)

### 1. 🧾 إدارة الفواتير والمبيعات (Invoices & POS)
- **إنشاء الفواتير المباشرة**: حساب الخصومات والإجماليات بدقة مع تحديث المخزون لحظياً.
- **تعديل السعر المباشر (Price Override)**: إمكانية تعديل سعر الصنف في السلة مع التحقق الأمني من صلاحية (`invoices.override_price`) أو موافقة المشرف.
- **تعليق واسترجاع السلات (Hold/Resume Invoices)**: إمكانية إيقاف الطلب مؤقتاً لحين انتهاء الزبون واستكمال عملية التسوق ثم استرجاعه أو إغلاقه.

### 2. ↩️ الإرجاع والتبديل (Returns & Exchange)
- **الإرجاع النقي (Pure Return)**: إرجاع كلي أو جزئي للأصناف مع إعادة الكميات للمخزون وحساب المبالغ المستردة.
- **التبديل الفوري (Product Exchange)**: إرجاع صنف وتعيين أصل بديلة في فاتورة جديدة في عملية واحدة متكاملة وذريّة (Atomic Transaction).

### 3. 👥 إدارة الموظفين والصلاحيات (Employees & Permissions)
- **إنشاء الموظفين الذري (Atomic Employee Creation)**: ربط الموظف بالدور والصلاحيات المناسبة فور إنشائه داخل `IDbTransaction` واحدة.
- **نظام الصلاحيات الدقيق**: التحكم الكامل في الوصول لجميع أجزاء النظام عبر صلاحيات منفصلة (`invoices.create`, `products.view`, `sales.create`, إلخ).

### 4. 🖨️ الطباعة الحرارية للفواتير (Printable HTML Receipt)
- توليد شفرة HTML مخصصة للطابعات الحرارية (80mm) ولأوراق A4 مباشرة من قاعدة البيانات.
- تطهير وترميز البيانات النصية ضد هجمات الخرق (XSS/Html Sanitize).

### 5. 📊 التقارير والإحصائيات (Reports & Analytics)
- تقارير المبيعات اليومية والدورية.
- تقارير أداء الكاشيرية وحضور الموظفين.
- تقارير حركة وتنقُّل المنتجات والمنتجات قريبة النفاد (Low Stock Alerts).

---

## 🚀 طريقة التشغيل (Setup & Execution)

### المتطلبات الأساسية:
- **.NET 10 SDK**
- **MySQL Server 8.0+**

### الخطوات:
1. قم بتهيئة قاعدة البيانات وإنشاء Schema باسم `supermarket_pos`.
2. قم بتحديث سلسلة الاتصال (Connection String) في ملف `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=supermarket_pos;Uid=root;Pwd=your_password;"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyHere_MustBeAtLeast32BytesLong!",
    "Issuer": "SupermarketApi",
    "Audience": "SupermarketClient",
    "ExpiryMinutes": 480
  }
}
```

3. تشغيل الـ Migration وبناء المشروع:
```bash
dotnet restore
dotnet build
dotnet run --project SupermarketSystem.Api.csproj
```

---

## 🔒 هيكلية الصلاحيات (Permissions Overview)

| مفتاح الصلاحية | الوصف |
| :--- | :--- |
| `invoices.create` | إنشاء الفواتير وإجراء عمليات البيع |
| `invoices.view` | استعراض قائمة الفواتير وتفاصيلها |
| `invoices.override_price` | تعديل أسعار المنتجات المباشرة أثناء البيع |
| `invoices.return` | تنفيذ عمليات الإرجاع واسترداد المبالغ |
| `invoices.exchange` | تنفيذ عمليات تبديل المنتجات |
| `products.view` | استعراض قائمة المنتجات |
| `products.create` / `products.update` | إضافة وتعديل أصناف المنتجات |
| `employees.create` / `employees.manage_permissions` | إدارة الموظفين وصلاحياتهم |

---

## 📝 ترخيص المشروع (License)

تم تطوير هذا المشروع لصالح نظام نقاط البيع بالسوبرماركت. جميع الحقوق محفوظة © 2026.