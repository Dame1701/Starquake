Opening Starquake for the first time
====================================

Starquake is free, and is not signed with a paid Apple Developer ID. The app
does carry a signature - Apple Silicon will not start a binary without one -
but it is an ad-hoc one, which is not the kind Gatekeeper trusts. So the first
time you open a copy that arrived from a browser, macOS says:

    "Apple could not verify Starquake is free of malware that may harm your
     Mac or compromise your privacy."

Nothing is wrong with the download. That message means macOS does not know who
published the app, and it only appears for files that came from the internet.

To install and open it:

  1. Drag  Starquake  onto the  Applications  folder in this window.

  2. Open Applications and double-click Starquake. The warning appears; click
     Done.

  3. Open  System Settings > Privacy & Security , scroll to the Security
     section, and click  Open Anyway  beside the message about Starquake.

  4. Confirm with  Open Anyway  and enter your password.

You only need to do this once. After that Starquake opens like any other app.

Right-clicking the app and choosing Open used to be the shortcut for this. It
has not worked since macOS 15 - the dialogue it produces no longer offers an
Open button - so System Settings is the way through now.


The one-line alternative
------------------------

If you would rather not go through System Settings, open the Terminal app
(Applications > Utilities > Terminal), paste this line, and press Return:

    xattr -dr com.apple.quarantine /Applications/Starquake.app

That removes the "downloaded from the internet" mark, and Starquake then opens
normally.


If macOS says the app is "damaged"
----------------------------------

It is not, and this should not happen with this build. If it does, the
download was most likely incomplete - delete the app and the .dmg, download
again, and repeat the steps above.
