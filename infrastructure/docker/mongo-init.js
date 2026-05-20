// Runs ONLY on first container start. The API also creates indexes on
// startup, so this script is intentionally minimal — it just ensures the
// database exists with empty collections.
db = db.getSiblingDB("serenauth");
const collections = [
  "organizations",
  "users",
  "providers",
  "patients",
  "prior_authorizations",
  "audit_events"
];
for (const name of collections) {
  if (!db.getCollectionNames().includes(name)) {
    db.createCollection(name);
  }
}
print("[serenauth] mongo-init complete");
