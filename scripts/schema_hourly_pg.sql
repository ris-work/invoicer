CREATE TABLE hourly_changes("datetime" TEXT, "itemcode" TEXT, "daydate" TEXT, "timehour" TEXT, "qty" TEXT, "qty_change" TEXT);
CREATE TABLE cost_import(itemcode TEXT, daydate TEXT, cost TEXT);
CREATE TABLE IF NOT EXISTS "public.cost_purchase_import"(
"itemcode" TEXT, "RUNNO" TEXT, "date" TEXT, "cost" TEXT);
CREATE TABLE IF NOT EXISTS "public.sih_import"(
"PLU_CODE" TEXT, "PLU_DESC" TEXT, "SIH" TEXT, "COSTVALUE" TEXT,
 "SELLVALUE" TEXT);
CREATE TABLE IF NOT EXISTS "selling_import"(
"code" TEXT, "sell" TEXT);
CREATE TABLE IF NOT EXISTS "prod_list_import"(
"dest" TEXT, "src" TEXT, "cost_src" TEXT, "proportion" TEXT);
CREATE TABLE inventory(itemcode TEXT NOT NULL, productname TEXT, PRIMARY KEY (itemcode));
CREATE TABLE IF NOT EXISTS "inventory_import"(
"itemcode" TEXT, "productname" TEXT);
CREATE TABLE IF NOT EXISTS "hourly_import"(
"itemcode" TEXT, "daydate" TEXT, "timehour" TEXT, "quantity" TEXT,
 "sumsell" TEXT, "sumcost" TEXT);
CREATE TABLE IF NOT EXISTS "tentative_revenue_import"(
"productcode" TEXT, "daydate" TEXT, "timehour" TEXT, "qty" TEXT,
 "sumsell" TEXT, "sumcost" TEXT);
CREATE TABLE tentative_revenue ("itemcode" TEXT, "daydate" TEXT, "timehour" TEXT, "quantity" TEXT, sumsell REAL DEFAULT 0, sumcost REAL DEFAULT 0, PRIMARY KEY (itemcode, daydate, timehour));
CREATE TABLE IF NOT EXISTS "full_inventory_current_import"(
"itemcode" TEXT, "sell" TEXT, "cost" TEXT);
CREATE TABLE full_inventory_current (itemcode INT, sell REAL, cost REAL, PRIMARY KEY (itemcode));
CREATE TABLE sih_history(datetime TEXT NOT NULL, itemcode INT NOT NULL, "desc" TEXT, sih INT, cost REAL, sell REAL, PRIMARY KEY (datetime, itemcode));
CREATE TABLE full_inventory_history (datetime TEXT NOT NULL, itemcode INT NOT NULL, sell REAL, cost REAL, primary key(datetime, itemcode));
CREATE TABLE sih_history_desc(datetime TEXT, itemcode INT, "desc" TEXT);
CREATE TABLE prod_list_history(
  what_happened TEXT,
  time TIMESTAMP WITH TIME ZONE,
  dest TEXT,
  src TEXT,
  cost_src TEXT,
  proportion TEXT
);
CREATE TABLE IF NOT EXISTS "hourly" ("itemcode" INT, "daydate" TEXT, "timehour" INT, "quantity" INT, sumsell REAL NOT NULL DEFAULT 0, sumcost REAL NOT NULL DEFAULT 0, PRIMARY KEY (itemcode, daydate, timehour));
CREATE TABLE sih_current(itemcode INT, "desc" TEXT, sih REAL, cost REAL, sell REAL, PRIMARY KEY (itemcode));
CREATE TABLE IF NOT EXISTS "selling" (itemcode INT, sell REAL, PRIMARY KEY (itemcode));
CREATE TABLE cost_purchase_2 (itemcode INT, runno INT, date TEXT, cost TEXT, PRIMARY KEY (itemcode));
CREATE TABLE IF NOT EXISTS "cost_purchase" (itemcode INT, runno INT, date TEXT, cost REAL, PRIMARY KEY (itemcode));
CREATE TABLE IF NOT EXISTS "cost"(itemcode INT, daydate TEXT, cost REAL, PRIMARY KEY(itemcode, daydate));
CREATE TABLE product_vendors_import(itemcode INT, vendorcode INT, cost REAL, sell REAL, PRIMARY KEY (itemcode, vendorcode));
CREATE TABLE product_vendors(itemcode INT, vendorcode INT, cost REAL, sell REAL, PRIMARY KEY (itemcode, vendorcode));
CREATE TABLE vendors_import(vendorcode INT, vendorname INT, PRIMARY KEY (vendorcode));
CREATE TABLE vendors(vendorcode INT, vendorname TEXT, PRIMARY KEY (vendorcode));
CREATE TABLE IF NOT EXISTS "prod_list"(dest INT NOT NULL, src INT NOT NULL, cost_src REAL NOT NULL, proportion REAL NOT NULL, PRIMARY KEY (dest, src));
CREATE INDEX tentative_revenue_everything ON tentative_revenue(itemcode, daydate, timehour, sumsell, sumcost);
CREATE INDEX full_inventory_current_covering ON full_inventory_current(itemcode, sell, cost);
CREATE INDEX sih_covering ON sih_current(itemcode, "desc", sih, cost, sell);
CREATE INDEX product_id_with_date_and_hour ON hourly (itemcode,daydate,timehour);
CREATE INDEX updated_dates ON hourly(daydate);
CREATE INDEX hourly_index_for_trends ON hourly(itemcode, daydate, quantity);
CREATE INDEX hourly_index_for_trends_replaceme ON hourly(daydate, itemcode, quantity);
--CREATE INDEX hourly_sales_averages ON hourly(itemcode, daydate, (sumsell/NULLIF(quantity,0)), (sumcost/NULLIF(quantity,0)));
CREATE INDEX hourly_covering ON hourly(itemcode, daydate, timehour, quantity, sumsell, sumcost);
CREATE INDEX hourly_sold_prices ON hourly(itemcode, daydate, (sumsell/NULLIF(quantity,0)), (sumcost/NULLIF(quantity,0)));
CREATE INDEX cost_purchase_itemcode ON cost_purchase(itemcode);
CREATE INDEX cost_purchase_covering ON cost_purchase(itemcode, runno, date, cost);
CREATE INDEX selling_covering ON selling(itemcode, sell);