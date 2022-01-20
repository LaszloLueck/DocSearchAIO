Installation 

There are multiple options where you can install the application.
The easiest




1. Ausführen wenn zu durchsuchendes Verzeichnis nicht das Std.- C-Laufwerk ist (bspw. ext. Festplatte). Leider ist die msg noch nicht soweit zeitnah neue wsl-Versionen bereitzustellen. Anyway!
 ````shell
  sudo mkdir /mnt/f --> wobei f irgendein Buchstabe sein kann
  sudo mount -t drvfs e: /mnt/f --> e ist der Windows Laufwerksbuchstabe und /mnt/x das oben erstellte Mount
  ````
  Jetzt sollte man mit einem `ls -lah /mnt/f` den Inhalt des Laufwerks sehen.
  Am besten wechselt man in das Verzeichnis auf dem Laufwerk das man indexieren möchte und führt dann einfach ein
  `pwd` aus. Dann kopiert man die Zeile und editiert nun die docker-compose.yaml im Zip-File.

  Hier wechselt man an die Sektion `volumes` der Container-Konfig `docsearchaio`

Und trägt an die Stelle `/mnt/f/scandir` das kopierte Verzeichnis ein:
````yaml
 volumes:
   - /mnt/f/scandir:/app/data
   - .comparer:/app/Resources/comparer
   - .config:/app/Resources/config
   - .statistics:/app/Resources/statistic
restart: always

````

2. Rechte für elastic-drive
Da man nicht bei jedem hochfahren der Docker-Container einen neuen Scan starten will, werden die Elastic-Daten als mouted volume außerhalb
des Containers gespeichert.
Damit das auch funktioniert folgende Schritte ausführen (für die Schritte muss man sich im gleichen Verzeichnis befinden in der auch die docker-compose.yaml liegt):
`````shell
mkdir .elasticData
sudo chown -R 1000:1000 .elasticData
`````

Damit werden die Besizer Rechte auf das Verzeichnis auf den aktuellen User gesetzt.
Jetzt kann man mal probieren mit einem

`docker-compose up` 

oder

`docker-compose up -d` -> für den Hintergrundmodus

die Container zu starten. Gerade bei erstmaligem Start ist es hilfreich den Log-Kram im Vordergrund ausgeben zu lassen (also ohne -d).
Für die Bedienung des Tools gibts ne teure Schulung, bitte bei mir melden!
