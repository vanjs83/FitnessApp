"""
Registracija smoke test za FitnessApp (Selenium 4).

Pokretanje:  python C:\\selenium\\register.py

VAZNO: za razliku od ostalih smoke skripti, OVA STVARA PRAVOG USERA u bazi.
Zato je default localhost (ne live), a email se generira jedinstven po pokretanju
(register+<timestamp>@smoke.local) pa nema "email vec postoji" kolizije.

Tok:
  1) REGISTER: tab "Registracija" -> popuni formu -> submit -> app uskoci u panel.
  2) LOGIN:    odjava (clear sesije) -> prijava istim podacima -> app opet u panel.
Tek ako oba koraka prodju, test je PASS. Time se potvrdjuje da je novi nalog
stvarno upotrebljiv za prijavu, a ne samo da je registracija prosla.

Selenium 4 sam dohvaca ChromeDriver (Selenium Manager) — ne treba rucna instalacija.
"""

import datetime

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException

# ── Konfiguracija ────────────────────────────────────────────────────────────
# Default localhost jer skripta STVARA usera. Za live otkomentiraj donji red.
BASE_URL = "http://localhost:5255/index.html"
# BASE_URL = "https://fitness-application.com/index.html"

# Jedinstven email po pokretanju -> nema kolizije s postojecim nalozima.
_STAMP = datetime.datetime.now().strftime("%Y%m%d%H%M%S")
FULL_NAME = "Smoke Test"
EMAIL = f"register+{_STAMP}@smoke.local"
PASSWORD = "Smoke123!"
ROLE = "Client"   # "Client" ili "Trainer"

TIMEOUT = 15  # sekundi za cekanje elemenata
# ─────────────────────────────────────────────────────────────────────────────


def make_driver():
    options = Options()
    # options.add_argument("--headless=new")   # otkomentiraj za rad bez prozora
    options.add_argument("--window-size=1280,900")
    options.add_experimental_option("excludeSwitches", ["enable-logging"])
    return webdriver.Chrome(options=options)


def run_register_test():
    driver = make_driver()
    wait = WebDriverWait(driver, TIMEOUT)
    try:
        # 1) Otvori app (login/register je u index.html, ne na "/")
        driver.get(BASE_URL)
        driver.execute_script("localStorage.clear();")
        driver.get(BASE_URL)

        # 2) Prebaci se na tab Registracija i pricekaj da se forma pojavi
        wait.until(EC.element_to_be_clickable(
            (By.CSS_SELECTOR, '.tab[data-tab="register"]'))).click()
        wait.until(EC.visibility_of_element_located((By.ID, "registerForm")))

        # 3) Odaberi ulogu. Radio je vizualno skriven (custom-styled role-picker),
        #    pa obican .click() baca ElementNotInteractableException -> koristi JS klik.
        role_radio = driver.find_element(
            By.CSS_SELECTOR, f'input[name="role"][value="{ROLE}"]')
        driver.execute_script("arguments[0].click();", role_radio)

        # 4) Popuni formu
        full_name = driver.find_element(By.ID, "registerFullName")
        email = driver.find_element(By.ID, "registerEmail")
        password = driver.find_element(By.ID, "registerPassword")
        full_name.clear()
        full_name.send_keys(FULL_NAME)
        email.clear()
        email.send_keys(EMAIL)
        password.clear()
        password.send_keys(PASSWORD)

        # 5) Submit
        driver.find_element(By.ID, "registerForm").submit()

        # 6) Dokaz uspjeha: app sakrije #authSection i pokaze #userInfo.
        #    Ako se umjesto toga pojavi greska u #registerError, prijavi FAIL.
        try:
            wait.until(EC.invisibility_of_element_located((By.ID, "authSection")))
            wait.until(EC.visibility_of_element_located((By.ID, "userInfo")))
        except TimeoutException:
            err = ""
            try:
                err = driver.find_element(By.ID, "registerError").text.strip()
            except Exception:
                pass
            print(f"[FAIL] REGISTER - nije se preslo na app. Poruka greske: {err or '(nema)'}")
            return False

        # 7) Potvrda da je nikao panel po ulozi
        panel = None
        for pid in ("adminPanel", "trainerPanel", "clientPanel"):
            try:
                if driver.find_element(By.ID, pid).is_displayed():
                    panel = pid
                    break
            except Exception:
                pass
        if panel is None:
            print("[FAIL] REGISTER - nijedan role panel se nije prikazao nakon registracije")
            return False

        print(f"[PASS] REGISTER - kreiran nalog: {EMAIL} (uloga {ROLE}, panel {panel})")

        # ── 2) LOGIN istim podacima ──────────────────────────────────────────
        # Odjava: ocisti sesiju i ponovo otvori app (garantirano natrag na auth formu).
        driver.execute_script("localStorage.clear();")
        driver.get(BASE_URL)

        # Login forma je default tab, ali za svaki slucaj klikni na njega.
        try:
            driver.find_element(By.CSS_SELECTOR, '.tab[data-tab="login"]').click()
        except Exception:
            pass

        login_email = wait.until(EC.visibility_of_element_located((By.ID, "loginEmail")))
        login_password = driver.find_element(By.ID, "loginPassword")
        login_email.clear()
        login_email.send_keys(EMAIL)
        login_password.clear()
        login_password.send_keys(PASSWORD)
        driver.find_element(By.ID, "loginForm").submit()

        try:
            wait.until(EC.invisibility_of_element_located((By.ID, "authSection")))
            wait.until(EC.visibility_of_element_located((By.ID, "userInfo")))
        except TimeoutException:
            err = ""
            try:
                err = driver.find_element(By.ID, "loginError").text.strip()
            except Exception:
                pass
            print(f"[FAIL] LOGIN - prijava novog naloga nije uspjela. Poruka greske: {err or '(nema)'}")
            return False

        print(f"[PASS] LOGIN - prijavljen kao: {EMAIL}")
        return True

    except TimeoutException:
        print(f"[TIMEOUT] forma se nije pojavila na {BASE_URL} "
              f"(provjeravas li slucajno '/' umjesto '/index.html'?)")
        return False
    finally:
        driver.quit()


if __name__ == "__main__":
    ok = run_register_test()
    raise SystemExit(0 if ok else 1)
