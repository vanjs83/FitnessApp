"""
Landing smoke test za FitnessApp (Selenium 4).

Pokretanje:  python C:\\selenium\\landing.py

Provjerava da se landing stranica (landing.html, servirana na "/") ispravno ucita:
naslov, hero naslov, glavni CTA i "Prijava" link na /index.html.

Selenium 4 sam dohvaca ChromeDriver (Selenium Manager) — ne treba rucna instalacija.
"""

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException, NoSuchElementException

# ── Konfiguracija ────────────────────────────────────────────────────────────
# VAZNO: landing stranica je na rootu "/" (login forma je u /index.html).
BASE_URL = "https://fitness-application.com/landing.html"
# BASE_URL = "http://localhost:5255/"   # za lokalno testiranje

TIMEOUT = 15  # sekundi za cekanje elemenata
# ─────────────────────────────────────────────────────────────────────────────


def make_driver():
    options = Options()
    # options.add_argument("--headless=new")   # otkomentiraj za rad bez prozora
    options.add_argument("--window-size=1280,900")
    options.add_experimental_option("excludeSwitches", ["enable-logging"])
    return webdriver.Chrome(options=options)


def run_landing_test():
    driver = make_driver()
    wait = WebDriverWait(driver, TIMEOUT)
    try:
        # 1) Otvori landing stranicu
        driver.get(BASE_URL)

        # 2) Naslov stranice mora sadrzavati "FitnessApp"
        try:
            wait.until(EC.title_contains("FitnessApp"))
        except TimeoutException:
            print(f"[FAIL] LANDING - naslov ne sadrzi 'FitnessApp' (dobiveno: '{driver.title}')")
            return False

        # 3) Hero naslov (h1) mora biti prisutan u DOM-u i imati teksta.
        #    NE koristimo visibility_of_* jer .hero-text ima klasu "reveal"
        #    (opacity:0 dok IntersectionObserver ne doda .visible) — Selenium ga
        #    tretira kao neprikazanog. textContent radi neovisno o animaciji.
        try:
            h1 = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, "section.hero h1")))
        except TimeoutException:
            print("[FAIL] LANDING - hero <h1> se nije pojavio (stranica se nije ucitala?)")
            return False
        h1_text = (h1.get_attribute("textContent") or "").strip()
        if not h1_text:
            print("[FAIL] LANDING - hero <h1> je prazan")
            return False

        # 4) Glavni CTA — "Registriraj se besplatno" link na /index.html
        try:
            driver.find_element(By.CSS_SELECTOR, "section.hero a.btn-cta[href='/index.html']")
        except NoSuchElementException:
            print("[FAIL] LANDING - nedostaje hero CTA link na /index.html")
            return False

        # 5) Nav "Prijava" link mora postojati i voditi na /index.html
        login_links = driver.find_elements(By.CSS_SELECTOR, "nav.nav a[href='/index.html']")
        if not login_links:
            print("[FAIL] LANDING - nedostaje 'Prijava' link u navigaciji")
            return False

        print(f"[PASS] LANDING - ucitana ('{driver.title}'), hero: \"{h1_text[:60]}...\"")
        return True

    except TimeoutException:
        print(f"[TIMEOUT] landing stranica se nije ucitala na {BASE_URL}")
        return False
    finally:
        driver.quit()


if __name__ == "__main__":
    ok = run_landing_test()
    raise SystemExit(0 if ok else 1)
