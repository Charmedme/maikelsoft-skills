# Answers of the product owner

- `rowVersion` is an opaque string for the client. The client sends back exactly what it got.
- The 409 body is a problem details object; the text is free.
- Delete does not need a version for now.
- The product audit log gets an entry only for the update that succeeded.
