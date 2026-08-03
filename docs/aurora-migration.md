# Aurora Serverless migration

## Objective

Move the hosted PostgreSQL workload from fixed RDS compute to a request-driven, private Aurora Serverless v2 cluster without changing application behaviour or weakening recovery controls.

## Migration sequence

1. Inventory application connection points and database extensions.
2. Create a backup of the source database.
3. Provision a private Aurora PostgreSQL-compatible target.
4. Restore or migrate schema and data through the controlled path.
5. compare table and record counts;
6. update the application connection boundary;
7. run application read and write checks;
8. observe idle and resumed runtime behaviour;
9. confirm backup and deletion controls; and
10. retire the former fixed instance only after acceptance.

## Acceptance checks

| Check | Accepted observation |
| --- | --- |
| Engine and status | Aurora PostgreSQL-compatible cluster available |
| Network | Private database boundary retained |
| Data | Record-count parity across the migration boundary |
| Application | Hosted read and write paths completed |
| Idle behaviour | Zero ACU observed while idle |
| Resume behaviour | Application recovered after idle and former-instance retirement |
| Recovery | Backup created before retirement |
| Protection | Storage encryption and deletion protection retained |

## Claim boundary

These observations establish migration completeness and the tested runtime state. They do not establish a fixed future bill, guaranteed latency under every load, or long-term savings. Cost and performance require observation over the actual workload.

No account number, resource ARN, endpoint, secret, security-group identifier, subnet, or connection string is published here.
