from datetime import date
from calculator import LiturgicalCalendar

# Initialize for the 2025–2026 church year
cal = LiturgicalCalendar(advent_year=2026)

# Lookup a specific Sunday or feast day
info = cal.lookup(date(2026, 10, 11))  # 2nd Sunday in Lent

print(info.get("readings"))
