# 02 — independent shared masters

`001-shared-master-data.sql` inserts shared lookup rows that do not depend on tenant, employee,
geography or permission rows. It covers device/client types, compliance and policy catalogues, UI
catalogues, email templates, gender, industry and tender status. It synchronizes the identities for
all tables it explicitly seeds.
