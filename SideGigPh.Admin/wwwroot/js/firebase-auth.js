// The operator's Google sign-in with the Firebase JS SDK (loaded from Google's CDN). C# reaches it only through
// FirebaseWebSignIn (Services/), behind IAdminSignIn: nothing else calls Firebase.
import { initializeApp } from "https://www.gstatic.com/firebasejs/11.0.2/firebase-app.js";
import {
    getAuth,
    GoogleAuthProvider,
    signInWithPopup,
    signOut as firebaseSignOut,
} from "https://www.gstatic.com/firebasejs/11.0.2/firebase-auth.js";

let auth = null;

const describe = (user) => user ? { uid: user.uid, email: user.email ?? null, name: user.displayName ?? null } : null;

/** Starts Firebase once and answers with whoever is still signed in from last time (or null). */
export async function start(config) {
    if (!auth) {
        auth = getAuth(initializeApp(config));
        await auth.authStateReady();
    }
    return describe(auth.currentUser);
}

/** The Google pop-up. Answers with the user, or throws with Firebase's error code (e.g. auth/popup-closed-by-user). */
export async function signIn() {
    const provider = new GoogleAuthProvider();
    provider.setCustomParameters({ prompt: "select_account" });
    const result = await signInWithPopup(auth, provider);
    return describe(result.user);
}

export async function signOut() {
    await firebaseSignOut(auth);
}

/** A fresh ID token for the API's Authorization header (the SDK refreshes it before it expires), or null. */
export async function idToken() {
    return auth?.currentUser ? await auth.currentUser.getIdToken() : null;
}
