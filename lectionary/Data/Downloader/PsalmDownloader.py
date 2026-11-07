import bg_scraper
import csv_builder
from tqdm import trange

VERSION = "KJV"
BOOK = "PSALM"
FILENAME = "Psalms_KJV.csv"
CSV_KEY_DELIMITER = ":"

for chapter in trange(1, 151, 1):
    verse = 1
    while True:
        reference = BOOK + " " + str(chapter) + ":" + str(verse)
        text = bg_scraper.fetch_pure_bible_text(reference, VERSION)
        if "not found" in text or "Error" in text:
            break
        else:
            key = (
                BOOK + CSV_KEY_DELIMITER + str(chapter) + CSV_KEY_DELIMITER + str(verse)
            )
            csv_builder.append_to_csv(key, text, FILENAME)
            verse = verse + 1
