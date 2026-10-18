import csv
import os


def append_to_csv(key, value, filename):
    file_exists = os.path.isfile(filename)
    with open(filename, mode="a", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        if not file_exists:
            writer.writerow(["Key", "Value"])
        writer.writerow([key, value])
