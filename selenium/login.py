"""
Login smoke test za FitnessApp (Selenium 4).

Pokretanje:  python C:\\selenium\\script.py

Selenium 4 sam dohvaća ChromeDriver (Selenium Manager) — ne treba ručna instalacija.
"""

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException

# ── Konfiguracija ────────────────────────────────────────────────────────────
# VAŽNO: login forma je u index.html, a NE na "/" (tamo je landing.html).
BASE_URL = "https://fitness-application.com/index.html"
# BASE_URL = "http://localhost:5255/index.html"   # za lokalno testiranje

# Stavi PRAVE podatke postojećeg test-naloga:
EMAIL = "admin@fitnessapp.com"
PASSWORD = "Admin123!"

TIMEOUT = 15  # sekundi za čekanje elemenata
# ─────────────────────────────────────────────────────────────────────────────


def make_driver():
    options = Options()
    # options.add_argument("--headless=new")   # otkomentiraj za rad bez prozora
    options.add_argument("--window-size=1280,900")
    options.add_experimental_option("excludeSwitches", ["enable-logging"])
    return webdriver.Chrome(options=options)


def run_login_test():
    driver = make_driver()
    wait = WebDriverWait(driver, TIMEOUT)
    try:
        # 1) Otvori login stranicu (index.html, ne landing)
        driver.get(BASE_URL)

        # 2) Pričekaj da se login forma pojavi i popuni je
        email = wait.until(EC.visibility_of_element_located((By.ID, "loginEmail")))
        password = driver.find_element(By.ID, "loginPassword")
        email.clear()
        email.send_keys(EMAIL)
        password.clear()
        password.send_keys(PASSWORD)

        # 3) Submit
        driver.find_element(By.ID, "loginForm").submit()

        # 4) Dokaz uspjeha: nakon login-a app sakrije #authSection i pokaže #userInfo.
        #    Ako se umjesto toga pojavi greška u #loginError, prijavi FAIL s razlogom.
        try:
            wait.until(EC.invisibility_of_element_located((By.ID, "authSection")))
            wait.until(EC.visibility_of_element_located((By.ID, "userInfo")))
        except TimeoutException:
            err = ""
            try:
                err = driver.find_element(By.ID, "loginError").text.strip()
            except Exception:
                pass
            print(f"[FAIL] LOGIN - nije se preslo na app. Poruka greske: {err or '(nema)'}")
            return False

        # 5) Dodatna potvrda — prikazani email u headeru
        shown = ""
        try:
            shown = driver.find_element(By.ID, "userEmail").text.strip()
        except Exception:
            pass
        print(f"[PASS] LOGIN - prijavljen kao: {shown or EMAIL}")
        return True

    except TimeoutException:
        print(f"[TIMEOUT] login forma se nije pojavila na {BASE_URL} "
              f"(provjeravas li slucajno '/' umjesto '/index.html'?)")
        return False
    finally:
        driver.quit()


if __name__ == "__main__":
    ok = run_login_test()
    raise SystemExit(0 if ok else 1)
