# Getting tarbaho.com found on Facebook, Instagram, TikTok and X

Social apps don't search the web the way Google does. People find a site through two things:

1. **The link on the profile**, put there by the account owner.
2. **The link preview**: the picture, title and text an app shows when the link is posted or messaged. These come
   from the `og:` and `twitter:` tags in `SideGigPh.FrontDoor/wwwroot/index.html`. They name the services and the
   three towns, and use the store's feature graphic (1024×500).

The page part is done. Everything below is set up in each app by whoever owns the accounts.

## Meta: Facebook, Messenger, Instagram, Threads

- [ ] **Facebook Page:** open the Page as admin, go to *Edit details* (or *About → Contact info*), and set *Website*
      to `https://www.tarbaho.com`.
- [ ] **Page button:** add the action button *Sign up*, linked to `https://www.tarbaho.com` (the page's "Get early access" button takes them
      to the form; a `#early-access` link doesn't scroll there, since the page draws itself after loading).
- [ ] **Verify the domain** so the Page owns tarbaho.com's link previews, can edit them in posts and can run ads
      later. In Meta Business Suite, go to *Settings → Brand safety and suitability → Domains → Add* `tarbaho.com`,
      and choose **DNS TXT record**. Add that record in Cloudflare DNS, then press *Verify*. Use the DNS record, not
      the meta-tag option: the record needs no change to the site.
- [ ] **Instagram:** switch to a *Professional* account, then go to *Edit profile → Links → Add external link* and
      enter `https://www.tarbaho.com`. Posts can't hold links, so use the bio link and the **Link sticker** in Stories.
- [ ] **Threads:** go to *Edit profile → Links* (it can also copy the Instagram bio link).
- [ ] **After any change to the title, text or picture:** open the Sharing Debugger at
      `developers.facebook.com/tools/debug`, paste `https://www.tarbaho.com/` and press **Scrape Again**. Facebook,
      Messenger and Instagram keep an old preview for weeks until someone does this.
- [ ] Pin a Page post that has the link.

## TikTok

- [ ] Switch the account to a **Business account** (*Settings → Account → Switch to Business Account*). A personal
      account may not have the *Website* field until it reaches a follower count.
- [ ] Go to *Edit profile → Website* and enter `https://www.tarbaho.com`.
- [ ] Videos can't hold a link: end each one with "Link in bio: tarbaho.com" on screen and in the caption.
- [ ] Ads later: TikTok Ads Manager asks to verify the domain the same way (DNS TXT in Cloudflare).

## X (Twitter)

- [ ] Go to *Edit profile → Website* and enter `https://www.tarbaho.com`.
- [ ] Pin a post that has the link. X draws the large card from the `twitter:` tags. It has no checker any more, so
      post the link once to see the card.

## Everywhere

- [ ] Write the address the same way everywhere: **`https://www.tarbaho.com`**, with www. Until Cloudflare
      redirects it, `tarbaho.com` without www doesn't open.
- [ ] Viber, Telegram and LinkedIn read the same `og:` tags, so a link shared there also shows the picture.
- [ ] The words in posts and bios can follow [`keywords.md`](keywords.md): "aircon cleaning Angono", "tubero",
      "pest control Rizal". Each app's own search finds posts and profiles by their words and hashtags
      (#Angono #Antipolo #Taytay #Rizal #aircon #tubero #pestcontrol).
