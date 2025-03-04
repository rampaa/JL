### Granting UIAccess privilege to JL

By editing the manifest file (`JL.exe.manifest`), you can change `uiAccess="false"` to `uiAccess="true"`. This allows JL to stay on top of other applications that do not have UIAccess, even without clipboard changes (e.g., JL will stay on top of Magpie before any clipboard changes occur).. It also enables JL to keep itself on top of applications that have UIAccess on every clipboard change. 

If you choose to grant JL UIAccess privilege, there are two conditions that must be met:

1. **Place JL in a Secure Location or Modify Group Policy**
    - You should either:
        1. Place JL in a "secure location" (e.g., "Program Files", "Program Files (x86)"), or
        2. Disable the **"Only elevate UIAccess applications that are installed in secure locations"** policy through **Group Policy**:
            - `Computer Configuration` → `Windows Settings` → `Security Settings` → `Local Policies` → `Security Options` → `User Account Control: Only elevate UIAccess applications that are installed in secure locations`.
    - If you choose to place JL in a secure location, you will need to **run JL as Administrator**; otherwise, JL may face issues.
    - If you choose to disable the policy, please read the details in this [documentation from Microsoft](https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/user-account-control-only-elevate-uiaccess-applications-that-are-installed-in-secure-locations) before proceeding.

2. **Sign JL with a Certificate**
    - You should either:
        1. **Create a certificate**, install it to your system, and sign JL with it, or
        2. **Install the provided certificate**.  
            *Note*: The provided certificate is a self-signed one. You can read more about self-signed certificates in this [Wikipedia article](https://en.wikipedia.org/wiki/Self-signed_certificate).  
    - If you choose to create your own certificate, here’s a helpful guide: [Guide for signing apps](https://support.smartbear.com/testcomplete/docs/working-with/automating/via-com/configuring-manifests.html#SignApp).
    - If you prefer to install the provided certificate, follow these steps:
        1. Right-click on `JL.exe` → `Properties` → `Digital Signatures` → Select "JL" from the "Signature List".
        2. Click `Details` → `View Certificate` → `Install Certificate`.
        3. Choose **Local Machine** → Select **"Place all certificates in the following store"** → Browse → Select **"Trusted Root Certification Authorities"** → Click `OK` → `Next` → `Finish`.
