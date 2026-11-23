# Tarbaho early-access Google Form (workers)

Ready to type into Google Forms. About 2–3 minutes to answer.

Form settings (Settings tab):
- **Collect email addresses: Do not collect** (we ask for the Play Store / Apple ID email ourselves, with a tip).
- **Limit to 1 response: off** (it would force a Google sign-in, which loses people).
- Presentation → confirmation message: see the end of this file.
- Make the link short (Send → link → "Shorten URL") and put it in place of `[GOOGLE FORM LINK]`.
- Responses → link to a Google Sheet; share the sheet only with the people who send the invites.

Sections (branching is set on question 3, "Go to section based on answer"):

1. **Section 1: Ikaw** (questions 1–3)
2. **Section 2: Android** (question 4a) → after section: *Continue to section 4*
3. **Section 3: iPhone** (question 4b) → after section: *Continue to section 4*
4. **Section 4: Trabaho mo** (questions 5–13) → *Submit form*

---

## Title

**Tarbaho: Early access para sa mga Skilled Worker**

## Description

```
Dito sa Tarbaho, makakahanap ka ng sarili mong customer sa presyong gusto mo.
Di pa bukas ung app. Angono, Antipolo at Taytay muna.

Fill up mo lang to para sa early access. Ung mga mauuna, sila unang makakapag-setup
ng profile pag bukas na ung app. Libre mag-sign up at mag-setup ng profile ngayon.
Mga 3 mins lang.

Privacy: Kukunin namin ung pangalan, number, email ng Play Store o Apple ID,
trade, bayan at iba pang sagot mo dito. Gagamitin lang namin to para i-contact ka
pag labas ng Tarbaho at para isend ung early access invite mo
(Google Play testing o TestFlight). Di namin to ibebenta o ibibigay sa iba.
Pwede mo ipabura anytime, message mo lang ung Tarbaho Facebook Page.
(Data Privacy Act of 2012, RA 10173)
```

---

## Section 1: Ikaw

**1. Pangalan mo** (Name)
- Type: Short answer
- Required: yes
- Helper: `Hal. Juan Dela Cruz`

**2. Mobile number** (Mobile number)
- Type: Short answer
- Required: yes
- Validation: Regular expression → Matches → `^(09|\+639)\d{9}$` → error text: `11-digit number dapat, hal. 09171234567`
- Helper: `Dito ka namin itetext pag bukas na ung app.`

**3. Anong phone mo?** (Which phone do you use?)
- Type: Multiple choice
- Required: yes
- Options (Android first) and "Go to section based on answer":
  - `Android (Samsung, Oppo, Vivo, Realme, Xiaomi, iba pa)` → **Go to Section 2 (Android)**
  - `iPhone` → **Go to Section 3 (iPhone)**

---

## Section 2: Android

Section description: `Para ma-install mo ung app bago pa sa lahat, kailangan namin ung Gmail na gamit mo sa Play Store.`

**4a. Gmail na gamit mo sa Play Store** (Google Play email)
- Type: Short answer
- Required: yes
- Validation: Text → Email address → error text: `Buong email dapat, hal. juan@gmail.com`
- Helper:
  ```
  Pano makita: buksan mo Play Store → tap mo ung picture mo sa taas, kanan →
  ung email na nandun, un ung ilagay mo dito.
  ```
- After section 2: **Continue to Section 4**

---

## Section 3: iPhone

Section description: `Sa iPhone, sa TestFlight ung early access (libreng app ng Apple).`

**4b. Apple ID email mo** (Apple ID email)
- Type: Short answer
- Required: yes
- Validation: Text → Email address
- Helper:
  ```
  Pano makita: Settings → tap mo ung pangalan mo sa pinakataas → ung email dun.
  Magsesend kami ng TestFlight invite. Install mo ung libreng "TestFlight" app sa App Store.
  ```
- After section 3: **Continue to Section 4**

---

## Section 4: Trabaho mo

**5. Ano ang trade mo?** (Your trade; tick all that apply)
- Type: Checkboxes
- Required: yes
- Options:
  - Aircon Technician
  - Construction: Foreman / maestro
  - Construction: Karpintero (Carpentry)
  - Construction: Electrical
  - Construction: Tubero (Plumbing)
  - Construction: Mason (Masonry)
  - Construction: Tiles
  - Construction: Welding
  - Construction: Bubong at alulod (Roofing & gutter)
  - Construction: Steelman (bakal / rebar)
  - Construction: Helper
  - Errands (linis ng bahay, grocery, PasaBuy, iba pa)
  - IT (computer, CCTV, internet)
  - Mekaniko (Car Mechanic)
  - Pest Control
  - Other: *(turn on "Other" so they can type)*

**6. Saang bayan ka pwedeng pumunta?** (Towns you go to; tick all)
- Type: Checkboxes
- Required: yes
- Options: `Angono`, `Taytay`, `Antipolo`, Other: *(on)*

**7. Barangay mo** (Your barangay)
- Type: Short answer
- Required: no
- Helper: `Hal. San Isidro, Angono`

**8. Ilang taon ka na sa trabaho mo?** (Years of experience)
- Type: Multiple choice
- Required: yes
- Options (same as the app): `Wala pang 1 taon`, `1–3 taon`, `3–10 taon`, `Mahigit 10 taon`

**9. Karaniwang presyo mo** (Your usual price range)
- Type: Short answer
- Required: no
- Helper: `Hal. ₱800–₱1,500 kada araw, o "depende sa trabaho". Ikaw bahala.`

**10. May TESDA certificate ka ba?** (TESDA certificate?)
- Type: Multiple choice
- Required: no
- Options: `Oo, meron`, `Wala pa`, `Nag-aaral pa lang`

**11. Facebook name mo** (Facebook name)
- Type: Short answer
- Required: no
- Helper: `Para madali ka namin ma-message.`

**12. Saan mo nalaman ang Tarbaho?** (How did you hear about us?)
- Type: Multiple choice
- Required: no
- Options: `Tarbaho Facebook Page`, `Facebook group`, `Kaibigan / kasama sa trabaho`, `Foreman o contractor`, Other: *(on)*

**13. Pahintulot** (Consent)
- Type: Checkboxes (one box)
- Required: yes
- Option:
  ```
  Okay lang sakin na gamitin ng Tarbaho ung mga sagot ko para i-contact ako
  pag labas ng app at para isend ung early access invite ko
  (Google Play testing o TestFlight). Alam ko pwede ko to ipabura anytime.
  ```

---

## Confirmation message (after Submit)

```
Salamat! Kasama ka na sa early access list.
Itetext o imemessage ka namin pag ready na ung invite mo.
Android: may link kang matatanggap para ma-install sa Play Store.
iPhone: may TestFlight invite na darating sa Apple ID email mo.
Share mo rin sa mga kasama mo sa site.
```

## How we use the answers (for the owner)

- **Android:** add each 4a email to the Play Console tester list (Internal testing allows up to 100 testers; use Closed testing with an email list or Google Group for more), then send them the opt-in link.
- **iPhone:** add each 4b email as an external tester in App Store Connect → TestFlight (needs one Beta App Review for the first build).
- Delete a person's row from the sheet (and the tester lists) when they ask, and delete the whole sheet once early access is over and the app is public.
- Keep the sheet private: it holds names, numbers and emails.
