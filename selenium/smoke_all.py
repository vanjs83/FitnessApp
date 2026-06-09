"""
FitnessApp - full smoke suite (Selenium 4), single file.

Runs read-only smoke checks across the whole app:
  - public pages (landing, login form)
  - per-role panels: it logs in, auto-detects the role (admin/trainer/client)
    from which panel appears, then opens every nav view and asserts that the
    view and a key element render.

It does NOT create/delete data - pure smoke, safe to run against live.

Run:
    python C:\\selenium\\smoke_all.py

Config: edit BASE_URL and ACCOUNTS below. Add the trainer/client accounts you
have; entries with an empty email are skipped. Role is detected automatically,
so the "label" is only cosmetic.

Selenium 4 fetches ChromeDriver automatically (Selenium Manager).
"""

import os
import time
import datetime

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

# ── Config ───────────────────────────────────────────────────────────────────
BASE_URL = "https://fitness-application.com"   # no trailing slash
# BASE_URL = "http://localhost:5255"           # for local testing

HEADLESS = False        # True = run without a window
TIMEOUT = 15            # seconds to wait for elements
STEP_PAUSE = 1.5        # seconds to linger on each view so you can watch it
SHOTS_DIR = r"C:\selenium\shots"

# Add every account you want covered. Role is auto-detected after login.
ACCOUNTS = [
    {"label": "admin",   "email": "admin@fitnessapp.com", "password": "Admin123!"},
    {"label": "trainer", "email": "tihomir.vanjurek@gmail.com","password": "demo12"},
    {"label": "client",  "email": "klijent.demo@fitness.local","password": "demo12"},
]

LANDING_URL = BASE_URL + "/"
APP_URL = BASE_URL + "/index.html"

# Per-panel view map: (nav-button css, view container id, key element css, label).
# The nav button is clicked, then we wait for the view and assert the key element.
CLIENT_VIEWS = [
    ('#clientPanel .nav-btn[data-view="workouts"]',    "workoutsView",    "#newWorkoutBtn",       "client/workouts"),
    ('#clientPanel .nav-btn[data-view="myPlans"]',     "myPlansView",     "#myPlansList",         "client/myPlans"),
    ('#clientPanel .nav-btn[data-view="myNutrition"]', "myNutritionView", "#myNutritionList",     "client/myNutrition"),
    ('#clientPanel .nav-btn[data-view="trainers"]',    "trainersView",    "#clientTrainersList",  "client/trainers"),
    ('#clientPanel .nav-btn[data-view="stats"]',       "statsView",       "#statsPlanSelect",     "client/stats"),
    ('#clientPanel .nav-btn[data-view="chat"]',        "chatView",        "#chatSidebar",         "client/chat"),
    ('#clientPanel .nav-btn[data-view="profile"]',     "profileView",     "#pfEmail",             "client/profile"),
]

TRAINER_VIEWS = [
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="clients"]',   "clientsView",    "#newClientBtn",        "trainer/clients"),
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="plans"]',     "plansView",      "#newPlanBtn",          "trainer/plans"),
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="nutrition"]', "nutritionView",  "#newNutritionPlanBtn", "trainer/nutrition"),
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="templates"]', "templatesView",  "#newTemplateBtn",      "trainer/templates"),
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="chat"]',      "chatView",       "#chatSidebar",         "trainer/chat"),
    ('#trainerPanel .trainer-nav-btn[data-trainer-view="profile"]',   "profileView",    "#pfEmail",             "trainer/profile"),
]

ADMIN_VIEWS = [
    ('#adminPanel .admin-tab[data-admin-tab="trainers"]', "adminTrainersTab", "#newTrainerBtn",     "admin/trainers"),
    ('#adminPanel .admin-tab[data-admin-tab="clients"]',  "adminClientsTab",  "#adminClientsList",  "admin/clients"),
    ('#adminPanel .admin-tab[data-admin-tab="mail"]',     "adminMailTab",     "#adminEmailSubject", "admin/mail"),
]

PANEL_VIEWS = {
    "adminPanel": ADMIN_VIEWS,
    "trainerPanel": TRAINER_VIEWS,
    "clientPanel": CLIENT_VIEWS,
}
# ─────────────────────────────────────────────────────────────────────────────


def make_driver():
    options = Options()
    options.add_argument("--window-size=1320,950")
    options.add_experimental_option("excludeSwitches", ["enable-logging"])
    if HEADLESS:
        options.add_argument("--headless=new")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
    return webdriver.Chrome(options=options)


def shot(driver, name):
    """Save a screenshot for evidence (used on failures)."""
    os.makedirs(SHOTS_DIR, exist_ok=True)
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    safe = name.replace("/", "_")
    path = os.path.join(SHOTS_DIR, f"{safe}_{ts}.png")
    try:
        driver.save_screenshot(path)
        print(f"        screenshot: {path}")
    except Exception:
        pass


# ── Public checks (no auth) ──────────────────────────────────────────────────

def test_landing(driver, wait):
    driver.get(LANDING_URL)
    wait.until(EC.title_contains("FitnessApp"))
    h1 = wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, "section.hero h1")))
    if not (h1.get_attribute("textContent") or "").strip():
        raise AssertionError("hero <h1> empty")
    driver.find_element(By.CSS_SELECTOR, "section.hero a.btn-cta[href='/index.html']")
    return True


def test_login_page(driver, wait):
    driver.get(APP_URL)
    wait.until(EC.visibility_of_element_located((By.ID, "loginForm")))
    driver.find_element(By.ID, "loginEmail")
    driver.find_element(By.ID, "loginPassword")
    driver.find_element(By.CSS_SELECTOR, '.tab[data-tab="register"]')
    return True


# ── Auth ─────────────────────────────────────────────────────────────────────

def login(driver, wait, email, password):
    """Fresh login. Returns the visible panel id (adminPanel/trainerPanel/clientPanel)."""
    driver.get(APP_URL)
    driver.execute_script("localStorage.clear();")
    driver.get(APP_URL)

    wait.until(EC.visibility_of_element_located((By.ID, "loginEmail"))).clear()
    driver.find_element(By.ID, "loginEmail").send_keys(email)
    driver.find_element(By.ID, "loginPassword").send_keys(password)
    driver.find_element(By.ID, "loginForm").submit()

    # Success: authSection hidden, userInfo shown.
    wait.until(EC.invisibility_of_element_located((By.ID, "authSection")))
    wait.until(EC.visibility_of_element_located((By.ID, "userInfo")))

    for pid in ("adminPanel", "trainerPanel", "clientPanel"):
        try:
            if driver.find_element(By.ID, pid).is_displayed():
                return pid
        except Exception:
            pass
    raise AssertionError("no role panel became visible after login")


def open_view(driver, wait, nav_css, view_id, key_css):
    """Click a nav button, wait for its view, assert a key element renders."""
    wait.until(EC.element_to_be_clickable((By.CSS_SELECTOR, nav_css))).click()
    wait.until(EC.visibility_of_element_located((By.ID, view_id)))
    driver.find_element(By.CSS_SELECTOR, key_css)
    return True


def test_settings_modal(driver, wait):
    """Settings modal is shared by all logged-in roles."""
    wait.until(EC.element_to_be_clickable((By.ID, "settingsBtn"))).click()
    wait.until(EC.visibility_of_element_located((By.ID, "settingsModal")))
    driver.find_element(By.ID, "changePasswordBtn")
    driver.find_element(By.ID, "closeSettingsBtn").click()
    wait.until(EC.invisibility_of_element_located((By.ID, "settingsModal")))
    return True


# ── Runner ───────────────────────────────────────────────────────────────────

def main():
    driver = make_driver()
    wait = WebDriverWait(driver, TIMEOUT)
    results = []  # (name, ok)

    def run(name, fn):
        try:
            fn()
            print(f"[PASS] {name}")
            results.append((name, True))
        except Exception as e:
            print(f"[FAIL] {name}: {type(e).__name__} - {e}")
            shot(driver, name)
            results.append((name, False))
        time.sleep(STEP_PAUSE)  # linger so the window is watchable

    try:
        # 1) Public
        run("public/landing", lambda: test_landing(driver, wait))
        run("public/login-page", lambda: test_login_page(driver, wait))

        # 2) Per account / role
        for acc in ACCOUNTS:
            if not acc.get("email"):
                continue
            label = acc["label"]
            panel = [None]

            def do_login():
                panel[0] = login(driver, wait, acc["email"], acc["password"])
            run(f"{label}/login", do_login)

            if panel[0] is None:
                continue

            for nav_css, view_id, key_css, vname in PANEL_VIEWS[panel[0]]:
                run(vname, lambda n=nav_css, v=view_id, k=key_css: open_view(driver, wait, n, v, k))

            run(f"{label}/settings", lambda: test_settings_modal(driver, wait))

    finally:
        driver.quit()

    # Summary
    passed = sum(1 for _, ok in results if ok)
    failed = len(results) - passed
    print("\n" + "=" * 48)
    print(f"  SMOKE SUMMARY: {passed} PASS / {failed} FAIL  (total {len(results)})")
    if failed:
        print("  Failed:")
        for name, ok in results:
            if not ok:
                print(f"    - {name}")
    print("=" * 48)
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
