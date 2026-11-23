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
Malapit na ang Tarbaho sa Android! Libreng app kung saan ang mga nagpapagawa
sa Angono, Taytay at Antipolo ang direktang hahanap sa'yo.
0% commission. Sariling presyo. Private ang number mo.

Sagutan ito para sa early access. Ang mga nauna, sila ang unang makakapag-setup
ng profile at unang makakatanggap ng requests pag bukas na ang app.
Mga 3 minuto lang.

Privacy: Kinukuha namin ang pangalan, mobile number, email ng Play Store o Apple ID,
trade, bayan at iba pang sagot mo dito. Gagamitin lang ito para i-contact ka
tungkol sa paglabas ng Tarbaho at para ipadala ang early-access invite mo
(Google Play testing o TestFlight). Hindi namin ito ibebenta o ibibigay sa iba.
Pwede mong ipabura anumang oras: mag-message sa Tarbaho Facebook Page.
(Data Privacy Act of 2012, RA 10173)
```

---

## Section 1: Ikaw

**1. Pangalan mo** (Name)
- Type: Short answer
- Required: yes
- Helper: `Halimbawa: Juan Dela Cruz`

**2. Mobile number** (Mobile number)
- Type: Short answer
- Required: yes
- Validation: Regular expression → Matches → `^(09|\+639)\d{9}$` → error text: `Pakilagay ang 11-digit number, hal. 09171234567`
- Helper: `Dito ka namin ite-text pag bukas na ang app.`

**3. Anong phone mo?** (Which phone do you use?)
- Type: Multiple choice
- Required: yes
- Options (Android first) and "Go to section based on answer":
  - `Android (Samsung, Oppo, Vivo, Realme, Xiaomi, iba pa)` → **Go to Section 2 (Android)**
  - `iPhone` → **Go to Section 3 (iPhone)**

---

## Section 2: Android

Section description: `Para maka-install ka bago pa lumabas ang app sa lahat, kailangan namin ang Gmail na gamit mo sa Play Store.`

**4a. Gmail na gamit mo sa Play Store** (Google Play email)
- Type: Short answer
- Required: yes
- Validation: Text → Email address → error text: `Pakilagay ang buong email, hal. juan@gmail.com`
- Helper:
  ```
  Paano makita: Buksan ang Play Store → i-tap ang picture mo sa taas, kanan →
  ang email na nakalagay doon ang ilagay mo dito.
  ```
- After section 2: **Continue to Section 4**

---

## Section 3: iPhone

Section description: `Sa iPhone, ang early access ay sa TestFlight (libreng app ng Apple).`

**4b. Apple ID email mo** (Apple ID email)
- Type: Short answer
- Required: yes
- Validation: Text → Email address
- Helper:
  ```
  Paano makita: Settings → i-tap ang pangalan mo sa pinakataas → ang email doon.
  Magpapadala kami ng TestFlight invite. I-install ang libreng "TestFlight" app sa App Store.
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
- Helper: `Halimbawa: San Isidro, Angono`

**8. Ilang taon ka na sa trabahong ito?** (Years of experience)
- Type: Multiple choice
- Required: yes
- Options (same as the app): `Wala pang 1 taon`, `1–3 taon`, `3–10 taon`, `Mahigit 10 taon`

**9. Karaniwang presyo mo** (Your usual price range)
- Type: Short answer
- Required: no
- Helper: `Halimbawa: ₱800–₱1,500 kada araw, o "depende sa trabaho". Ikaw ang bahala.`

**10. May TESDA certificate ka ba?** (TESDA certificate?)
- Type: Multiple choice
- Required: no
- Options: `Oo, meron`, `Wala pa`, `Nag-aaral pa lang`

**11. Facebook name mo** (Facebook name)
- Type: Short answer
- Required: no
- Helper: `Para madali ka naming ma-message.`

**12. Saan mo nalaman ang Tarbaho?** (How did you hear about us?)
- Type: Multiple choice
- Required: no
- Options: `Tarbaho Facebook Page`, `Facebook group`, `Kaibigan / kasama sa trabaho`, `Foreman o contractor`, Other: *(on)*

**13. Pahintulot** (Consent)
- Type: Checkboxes (one box)
- Required: yes
- Option:
  ```
  Pumapayag ako na gamitin ng Tarbaho ang mga sagot ko para i-contact ako
  tungkol sa paglabas ng app at ipadala ang early-access invite ko
  (Google Play testing o TestFlight). Alam kong pwede kong ipabura ito anumang oras.
  ```

---

## Confirmation message (after Submit)

```
Salamat, Kuya / Ate! Nasa listahan ka na ng early access. 🎉
Ite-text o ime-message ka namin pag handa na ang invite mo.
Android: makakatanggap ka ng link para i-install mula sa Play Store.
iPhone: makakatanggap ka ng TestFlight invite sa Apple ID email mo.
I-share ang form sa kasamahan mo sa site!
```

## How we use the answers (for the owner)

- **Android:** add each 4a email to the Play Console tester list (Internal testing allows up to 100 testers; use Closed testing with an email list or Google Group for more), then send them the opt-in link.
- **iPhone:** add each 4b email as an external tester in App Store Connect → TestFlight (needs one Beta App Review for the first build).
- Delete a person's row from the sheet (and the tester lists) when they ask, and delete the whole sheet once early access is over and the app is public.
- Keep the sheet private: it holds names, numbers and emails.
