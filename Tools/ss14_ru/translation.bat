pip install -r requirements.txt --no-warn-script-location
python ./yamlextractor.py
python ./keyfinder.py
python ./clean_duplicates.py
python ./clean_empty.py

PAUSE
