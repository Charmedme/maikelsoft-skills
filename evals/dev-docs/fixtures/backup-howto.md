# Back up the database

This guide makes a backup of the orders database.

## Steps

1. Stop the export job; it locks the tables.
2. Run the backup command:

   ```sh
   pg_dump -Fc orders > orders.dump
   ```

3. Copy `orders.dump` to the backup bucket.
4. Start the export job again.
