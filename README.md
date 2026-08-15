<div dir="rtl">

# ⌨️ KeyboardFixer — مصحّح لوحة المفاتيح العربي/الإنجليزي

**أداة نظام خفية تعمل في الخلفية على Windows، تحوّل النص الذي كتبته بالغلط من لغة إلى أخرى بضغطة زر واحدة.**

---

## 🎯 المشكلة التي تحلّها

هل حدث لك هذا؟

```
أردت أن تكتب: السلام عليكم
فكتبت:         hghsghl ugd;l
```

أو العكس:
```
أردت أن تكتب: Hello World
فكتبت:         هيممخ صخقمي
```

**KeyboardFixer** يصلح هذا فوراً، في أي تطبيق، بدون أي تدخل إضافي.

---

</div>

## 📺 Demo

```
قبل التحويل:    hghsghl ugd;l
اضغط Alt+Z  →
بعد التحويل:    السلام عليكم
```

```
Before:    السلام عليكم في هذا العالم الجميل
Press Alt+Z  →
After:     hghsghl ugd;l td i`h hgughl hg[ldg
```

---

<div dir="rtl">

## ✨ المميزات

| الميزة | التفاصيل |
|--------|----------|
| 🌍 **يعمل في كل مكان** | Notepad، Word، Chrome، VS Code، WhatsApp Web، برامج ERP القديمة |
| ⚡ **آني** | التحويل يحدث في أقل من 200ms |
| 🧠 **ذكي** | يكتشف اتجاه التحويل تلقائياً (AR→EN أو EN→AR) |
| 🔡 **يتعامل مع Caps Lock** | يصلح الأحرف الكبيرة المكتوبة بالخطأ تلقائياً |
| 🔢 **لا يمس الأرقام والرموز** | `123 !@#` تمر بدون تغيير |
| 🏭 **يدعم برامج ERP** | يستخدم Win32 Messages للبرامج القديمة |
| 🔇 **خفي تماماً** | يعمل في System Tray فقط، بدون نوافذ |
| 🚀 **يبدأ مع Windows** | يسجل نفسه تلقائياً في Registry |
| 📦 **صغير الحجم** | EXE واحد أقل من 100KB، بدون تثبيت |

---

## ⌨️ اختصارات لوحة المفاتيح

| الاختصار | الوظيفة |
|----------|---------|
| `Alt + Z` | تحويل النص المظلل |
| `Ctrl + Shift + X` | بديل في حالة تعارض Alt+Z |

---

## 🚀 التثبيت السريع (خطوتان فقط)

### الخطوة 1 — تنزيل وبناء

**أسهل طريقة (PowerShell — بنقرة واحدة):**

```powershell
# افتح PowerShell كمستخدم عادي (بدون Admin) ثم انسخ هذا الأمر:
Set-ExecutionPolicy -Scope CurrentUser Bypass -Force
git clone https://github.com/YOUR_USERNAME/keyboard-layout-fixer.git
cd keyboard-layout-fixer
.\build.ps1 -Install
```

هذا الأمر سيقوم تلقائياً بـ:
- ✅ بناء الملف التنفيذي `KeyboardFixer.exe`
- ✅ نسخه إلى مجلد دائم في جهازك
- ✅ تسجيله ليبدأ مع Windows
- ✅ إنشاء اختصار على سطح المكتب
- ✅ تشغيله فوراً

---

### الخطوة 2 — التحقق من التثبيت

بعد التثبيت ستلاحظ:
- 🔔 إشعار في أسفل يمين الشاشة: *"مصحح الكيبورد يعمل"*
- ⌨️ أيقونة صغيرة بجانب الساعة في System Tray

**الاختبار السريع:**
1. افتح المفكرة (Notepad)
2. اكتب: `hghsghl ugd;l`
3. ظلل النص
4. اضغط **`Alt + Z`**
5. يتحول فوراً إلى: **`السلام عليكم`** 🎉

---

## 🏗️ البناء يدوياً (للمطورين)

**المتطلبات:**
- Windows 7 / 10 / 11
- .NET Framework 4.x (مثبت افتراضياً على كل Windows)
- لا يحتاج Visual Studio

```powershell
# بناء فقط (بدون تثبيت)
.\build.ps1

# بناء + تثبيت + تشغيل
.\build.ps1 -Install
```

الملف الناتج: `bin\KeyboardFixer.exe`

---

## 📁 هيكل المشروع

```
keyboard-layout-fixer/
│
├── src/
│   └── KeyboardFixer.cs       # كل الكود في ملف واحد
│
├── docs/
│   └── keyboard-map.md        # خريطة كاملة للمفاتيح AR/EN
│
├── bin/                       # (مجلد مُولَّد — لا يُرفع لـ GitHub)
│   └── KeyboardFixer.exe
│
├── build.ps1                  # سكريبت البناء والتثبيت
├── .gitignore
├── LICENSE                    # MIT
└── README.md
```

---

## ⚙️ كيف تعمل الأداة من الداخل

```
المستخدم يضغط Alt+Z
        │
        ▼
SetWindowsHookEx (WH_KEYBOARD_LL)
يعترض الضغطة على مستوى النظام
        │
        ▼
تحرير كل مفاتيح التعديل (Alt, Ctrl, Shift)
        │
        ▼
محاولة 1: Ctrl+C (نسخ التحديد)
        │
        ▼ فشل? (برامج ERP/Legacy)
محاولة 2: WM_COPY مباشرة للنافذة المُفعّلة
(عبر AttachThreadInput + SendMessage)
        │
        ▼
خوارزمية التحويل الذكية:
  - تحليل النص: هل غالبيته عربي أم إنجليزي؟
  - تطبيق خريطة المفاتيح بالكامل
  - معالجة Caps Lock التلقائية
  - الحفاظ على الأرقام والرموز
        │
        ▼
وضع النص المحوّل في الحافظة (Clipboard)
        │
        ▼
محاولة 1: Ctrl+V (لصق)
محاولة 2: WM_PASTE للنافذة مباشرة
        │
        ▼
استعادة محتوى الحافظة الأصلي
+ صوت تأكيد
```

---

## 🔒 الخصوصية والأمان

- **لا اتصال بالإنترنت** — الأداة تعمل محلياً بالكامل
- **لا تسجيل للضغطات** — الـ Hook يراقب فقط اختصار `Alt+Z`
- **لا قاعدة بيانات** — لا ملفات إضافية، ملف EXE واحد فقط
- **مفتوح المصدر** — يمكنك مراجعة كل سطر من الكود

---

## 🛠️ إزالة التثبيت (Uninstall)

```powershell
# إلغاء التسجيل من بدء التشغيل
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "KeyboardFixer" -ErrorAction SilentlyContinue

# حذف الملفات
Remove-Item "$env:LOCALAPPDATA\KeyboardFixer" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:USERPROFILE\Desktop\KeyboardFixer.lnk" -Force -ErrorAction SilentlyContinue

Write-Host "✅ Uninstalled successfully"
```

---

## 🗺️ خريطة المفاتيح الكاملة

[📄 انظر docs/keyboard-map.md](docs/keyboard-map.md)

---

## 🤝 المساهمة

المساهمات مرحب بها! انظر [CONTRIBUTING.md](CONTRIBUTING.md)

**أفكار للتطوير المستقبلي:**
- [ ] دعم لغات إضافية (فارسي، أردو)
- [ ] إعدادات قابلة للتخصيص عبر واجهة رسومية
- [ ] تغيير الاختصار من الإعدادات
- [ ] إحصائيات عدد مرات الاستخدام
- [ ] تثبيت عبر `winget`

---

## 📄 الرخصة

[MIT](LICENSE) © 2026

---

<div align="center">

**إذا أفادتك الأداة، ضع ⭐ على المستودع!**

</div>
