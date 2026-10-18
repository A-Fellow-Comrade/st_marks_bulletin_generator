import requests
from bs4 import BeautifulSoup
import urllib.parse
import re


def fetch_pure_bible_text(reference: str, version: str = "ESV") -> str:
    encoded_ref = urllib.parse.quote(reference)
    url = (
        f"https://www.biblegateway.com/passage/?search={encoded_ref}&version={version}"
    )

    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
    }

    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")
        passage_container = soup.find("div", class_="passage-content")

        if not passage_container:
            return "Passage not found."

        # The ultimate blacklist of junk elements
        unwanted_selectors = [
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",  # Section headings
            ".versenum",
            ".chapternum",  # Verse and chapter numbers
            ".crossreference",
            ".footnote",
            ".footnotes",  # Inline study notes
            ".crossrefs",  # Bottom cross-reference master list
            ".passage-bottom",  # Bottom UI wrapper
            ".passage-other-trans",  # "in all English translations" area
            "a",  # ALL hyperlinks (kills "Read full chapter")
        ]

        # Destroy all unwanted elements
        for selector in unwanted_selectors:
            for element in passage_container.select(selector):
                element.decompose()

        # Grab the remaining text
        raw_text = passage_container.get_text(separator=" ")

        # Clean up whitespace
        clean_text = re.sub(r"\s+", " ", raw_text).strip()
        clean_text = re.sub(r"\[\s*[a-zA-Z]?\s*\]", "", clean_text)

        return re.sub(r"\s+", " ", clean_text).strip()

    except requests.exceptions.RequestException as e:
        return f"Error: {e}"
