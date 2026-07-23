---
hide:
    - toc
---
One print language offered for the Paint Thickness Certificate: the language code and the
 signed public URL that serves the certificate in that language. The issuing host owns the
 certificate templates, so it declares the supported languages by which entries it returns —
 UIs render a print menu from the list as-is (the order is the display order).

| Property | Summary |
|----------|---------|
| Language <div><strong>``string``</strong></div> | The language code the certificate prints in (e.g. "en", "ar", "ku"). |
| Name <div><strong>``string``</strong></div> | The language's display name for the print menu — conventionally its native name (e.g. "English", "العربية", "کوردی"). Supplied by the issuing host along with the URL so UIs render the menu with zero language knowledge of their own. |
| Url <div><strong>``string``</strong></div> | The signed public URL serving the certificate in this language. |
