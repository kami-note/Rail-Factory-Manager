ngrok:
	ngrok http --url apparent-driving-horse.ngrok-free.app 5082

deadcode-csharp:
	./scripts/find-dead-code-csharp.sh

deadcode-csharp-heuristic:
	./scripts/find-dead-code-csharp-heuristic.sh

db-reset:
	./scripts/db-reset-dev.sh
