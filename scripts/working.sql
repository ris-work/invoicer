PGDMP  ;    2                |            hourly    16.3    16.3 _    r           0    0    ENCODING    ENCODING        SET client_encoding = 'UTF8';
                      false            s           0    0 
   STDSTRINGS 
   STDSTRINGS     (   SET standard_conforming_strings = 'on';
                      false            t           0    0 
   SEARCHPATH 
   SEARCHPATH     8   SELECT pg_catalog.set_config('search_path', '', false);
                      false            u           1262    16397    hourly    DATABASE     �   CREATE DATABASE hourly WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'English_United Kingdom.1252';
    DROP DATABASE hourly;
                postgres    false            v           0    0    DATABASE hourly    ACL     '   GRANT ALL ON DATABASE hourly TO rishi;
                   postgres    false    4981            w           0    0    SCHEMA public    ACL     7   GRANT ALL ON SCHEMA public TO rishi WITH GRANT OPTION;
                   pg_database_owner    false    5            �            1259    16399    a    TABLE     )   CREATE TABLE public.a (
    b integer
);
    DROP TABLE public.a;
       public         heap    rishi    false            �            1259    16580    cnta    VIEW     �   CREATE VIEW public.cnta AS
 WITH RECURSIVE cnta(x) AS (
         SELECT 0 AS "?column?"
        UNION ALL
         SELECT (cnta_1.x + 1)
           FROM cnta cnta_1
          WHERE (cnta_1.x < 1000)
        )
 SELECT x
   FROM cnta;
    DROP VIEW public.cnta;
       public          rishi    false            �            1259    16532    cost    TABLE     f   CREATE TABLE public.cost (
    itemcode integer NOT NULL,
    daydate text NOT NULL,
    cost real
);
    DROP TABLE public.cost;
       public         heap    rishi    false            �            1259    16407    cost_import    TABLE     X   CREATE TABLE public.cost_import (
    itemcode text,
    daydate text,
    cost text
);
    DROP TABLE public.cost_import;
       public         heap    rishi    false            �            1259    16525    cost_purchase    TABLE     v   CREATE TABLE public.cost_purchase (
    itemcode integer NOT NULL,
    runno integer,
    date text,
    cost real
);
 !   DROP TABLE public.cost_purchase;
       public         heap    rishi    false            �            1259    16518    cost_purchase_2    TABLE     x   CREATE TABLE public.cost_purchase_2 (
    itemcode integer NOT NULL,
    runno integer,
    date text,
    cost text
);
 #   DROP TABLE public.cost_purchase_2;
       public         heap    rishi    false            �            1259    25253    dates    VIEW     �   CREATE VIEW public.dates AS
 WITH RECURSIVE day(x) AS (
         VALUES ('2019-01-01'::date)
        UNION ALL
         SELECT (day_1.x + 1)
           FROM day day_1
          WHERE (day_1.x < date(now()))
        )
 SELECT x
   FROM day;
    DROP VIEW public.dates;
       public          rishi    false            �            1259    16468    full_inventory_current    TABLE     l   CREATE TABLE public.full_inventory_current (
    itemcode integer NOT NULL,
    sell real,
    cost real
);
 *   DROP TABLE public.full_inventory_current;
       public         heap    rishi    false            �            1259    16463    full_inventory_current_import    TABLE     g   CREATE TABLE public.full_inventory_current_import (
    itemcode text,
    sell text,
    cost text
);
 1   DROP TABLE public.full_inventory_current_import;
       public         heap    rishi    false            �            1259    16480    full_inventory_history    TABLE     �   CREATE TABLE public.full_inventory_history (
    datetime text NOT NULL,
    itemcode integer NOT NULL,
    sell real,
    cost real
);
 *   DROP TABLE public.full_inventory_history;
       public         heap    rishi    false            �            1259    16497    hourly    TABLE     �   CREATE TABLE public.hourly (
    itemcode integer NOT NULL,
    daydate text NOT NULL,
    timehour integer NOT NULL,
    quantity integer,
    sumsell real DEFAULT 0 NOT NULL,
    sumcost real DEFAULT 0 NOT NULL
);
    DROP TABLE public.hourly;
       public         heap    rishi    false            �            1259    16402    hourly_changes    TABLE     �   CREATE TABLE public.hourly_changes (
    datetime text,
    itemcode text,
    daydate text,
    timehour text,
    qty text,
    qty_change text
);
 "   DROP TABLE public.hourly_changes;
       public         heap    rishi    false            �            1259    16444    hourly_import    TABLE     �   CREATE TABLE public.hourly_import (
    itemcode text,
    daydate text,
    timehour text,
    quantity text,
    sumsell text,
    sumcost text
);
 !   DROP TABLE public.hourly_import;
       public         heap    rishi    false            �            1259    16432 	   inventory    TABLE     T   CREATE TABLE public.inventory (
    itemcode text NOT NULL,
    productname text
);
    DROP TABLE public.inventory;
       public         heap    rishi    false            �            1259    16439    inventory_import    TABLE     R   CREATE TABLE public.inventory_import (
    itemcode text,
    productname text
);
 $   DROP TABLE public.inventory_import;
       public         heap    rishi    false            �            1259    16561 	   prod_list    TABLE     �   CREATE TABLE public.prod_list (
    dest integer NOT NULL,
    src integer NOT NULL,
    cost_src real NOT NULL,
    proportion real NOT NULL
);
    DROP TABLE public.prod_list;
       public         heap    rishi    false            �            1259    16492    prod_list_history    TABLE     �   CREATE TABLE public.prod_list_history (
    what_happened text,
    "time" timestamp with time zone,
    dest text,
    src text,
    cost_src text,
    proportion text
);
 %   DROP TABLE public.prod_list_history;
       public         heap    rishi    false            �            1259    16427    prod_list_import    TABLE     n   CREATE TABLE public.prod_list_import (
    dest text,
    src text,
    cost_src text,
    proportion text
);
 $   DROP TABLE public.prod_list_import;
       public         heap    rishi    false            �            1259    16544    product_vendors    TABLE     �   CREATE TABLE public.product_vendors (
    itemcode integer NOT NULL,
    vendorcode integer NOT NULL,
    cost real,
    sell real
);
 #   DROP TABLE public.product_vendors;
       public         heap    rishi    false            �            1259    16539    product_vendors_import    TABLE     �   CREATE TABLE public.product_vendors_import (
    itemcode integer NOT NULL,
    vendorcode integer NOT NULL,
    cost real,
    sell real
);
 *   DROP TABLE public.product_vendors_import;
       public         heap    rishi    false            �            1259    16412    public.cost_purchase_import    TABLE     y   CREATE TABLE public."public.cost_purchase_import" (
    itemcode text,
    "RUNNO" text,
    date text,
    cost text
);
 1   DROP TABLE public."public.cost_purchase_import";
       public         heap    rishi    false            �            1259    16417    public.sih_import    TABLE     �   CREATE TABLE public."public.sih_import" (
    "PLU_CODE" text,
    "PLU_DESC" text,
    "SIH" text,
    "COSTVALUE" text,
    "SELLVALUE" text
);
 '   DROP TABLE public."public.sih_import";
       public         heap    rishi    false            �            1259    16513    selling    TABLE     N   CREATE TABLE public.selling (
    itemcode integer NOT NULL,
    sell real
);
    DROP TABLE public.selling;
       public         heap    rishi    false            �            1259    16422    selling_import    TABLE     E   CREATE TABLE public.selling_import (
    code text,
    sell text
);
 "   DROP TABLE public.selling_import;
       public         heap    rishi    false            �            1259    16506    sih_current    TABLE     �   CREATE TABLE public.sih_current (
    itemcode integer NOT NULL,
    "desc" text,
    sih real,
    cost real,
    sell real
);
    DROP TABLE public.sih_current;
       public         heap    rishi    false            �            1259    16473    sih_history    TABLE     �   CREATE TABLE public.sih_history (
    datetime text NOT NULL,
    itemcode integer NOT NULL,
    "desc" text,
    sih integer,
    cost real,
    sell real
);
    DROP TABLE public.sih_history;
       public         heap    rishi    false            �            1259    16487    sih_history_desc    TABLE     c   CREATE TABLE public.sih_history_desc (
    datetime text,
    itemcode integer,
    "desc" text
);
 $   DROP TABLE public.sih_history_desc;
       public         heap    rishi    false            �            1259    16454    tentative_revenue    TABLE     �   CREATE TABLE public.tentative_revenue (
    itemcode text NOT NULL,
    daydate text NOT NULL,
    timehour text NOT NULL,
    quantity text,
    sumsell real DEFAULT 0,
    sumcost real DEFAULT 0
);
 %   DROP TABLE public.tentative_revenue;
       public         heap    rishi    false            �            1259    16449    tentative_revenue_import    TABLE     �   CREATE TABLE public.tentative_revenue_import (
    productcode text,
    daydate text,
    timehour text,
    qty text,
    sumsell text,
    sumcost text
);
 ,   DROP TABLE public.tentative_revenue_import;
       public         heap    rishi    false            �            1259    16554    vendors    TABLE     V   CREATE TABLE public.vendors (
    vendorcode integer NOT NULL,
    vendorname text
);
    DROP TABLE public.vendors;
       public         heap    rishi    false            �            1259    16549    vendors_import    TABLE     `   CREATE TABLE public.vendors_import (
    vendorcode integer NOT NULL,
    vendorname integer
);
 "   DROP TABLE public.vendors_import;
       public         heap    rishi    false            S          0    16399    a 
   TABLE DATA              COPY public.a (b) FROM stdin;
    public          rishi    false    215   �g       j          0    16532    cost 
   TABLE DATA           7   COPY public.cost (itemcode, daydate, cost) FROM stdin;
    public          rishi    false    238   �g       U          0    16407    cost_import 
   TABLE DATA           >   COPY public.cost_import (itemcode, daydate, cost) FROM stdin;
    public          rishi    false    217   �g       i          0    16525    cost_purchase 
   TABLE DATA           D   COPY public.cost_purchase (itemcode, runno, date, cost) FROM stdin;
    public          rishi    false    237   �g       h          0    16518    cost_purchase_2 
   TABLE DATA           F   COPY public.cost_purchase_2 (itemcode, runno, date, cost) FROM stdin;
    public          rishi    false    236   h       `          0    16468    full_inventory_current 
   TABLE DATA           F   COPY public.full_inventory_current (itemcode, sell, cost) FROM stdin;
    public          rishi    false    228   9h       _          0    16463    full_inventory_current_import 
   TABLE DATA           M   COPY public.full_inventory_current_import (itemcode, sell, cost) FROM stdin;
    public          rishi    false    227   Vh       b          0    16480    full_inventory_history 
   TABLE DATA           P   COPY public.full_inventory_history (datetime, itemcode, sell, cost) FROM stdin;
    public          rishi    false    230   sh       e          0    16497    hourly 
   TABLE DATA           Y   COPY public.hourly (itemcode, daydate, timehour, quantity, sumsell, sumcost) FROM stdin;
    public          rishi    false    233   �h       T          0    16402    hourly_changes 
   TABLE DATA           `   COPY public.hourly_changes (datetime, itemcode, daydate, timehour, qty, qty_change) FROM stdin;
    public          rishi    false    216   �h       \          0    16444    hourly_import 
   TABLE DATA           `   COPY public.hourly_import (itemcode, daydate, timehour, quantity, sumsell, sumcost) FROM stdin;
    public          rishi    false    224   �h       Z          0    16432 	   inventory 
   TABLE DATA           :   COPY public.inventory (itemcode, productname) FROM stdin;
    public          rishi    false    222   �h       [          0    16439    inventory_import 
   TABLE DATA           A   COPY public.inventory_import (itemcode, productname) FROM stdin;
    public          rishi    false    223   i       o          0    16561 	   prod_list 
   TABLE DATA           D   COPY public.prod_list (dest, src, cost_src, proportion) FROM stdin;
    public          rishi    false    243   !i       d          0    16492    prod_list_history 
   TABLE DATA           c   COPY public.prod_list_history (what_happened, "time", dest, src, cost_src, proportion) FROM stdin;
    public          rishi    false    232   >i       Y          0    16427    prod_list_import 
   TABLE DATA           K   COPY public.prod_list_import (dest, src, cost_src, proportion) FROM stdin;
    public          rishi    false    221   [i       l          0    16544    product_vendors 
   TABLE DATA           K   COPY public.product_vendors (itemcode, vendorcode, cost, sell) FROM stdin;
    public          rishi    false    240   xi       k          0    16539    product_vendors_import 
   TABLE DATA           R   COPY public.product_vendors_import (itemcode, vendorcode, cost, sell) FROM stdin;
    public          rishi    false    239   �i       V          0    16412    public.cost_purchase_import 
   TABLE DATA           V   COPY public."public.cost_purchase_import" (itemcode, "RUNNO", date, cost) FROM stdin;
    public          rishi    false    218   �i       W          0    16417    public.sih_import 
   TABLE DATA           f   COPY public."public.sih_import" ("PLU_CODE", "PLU_DESC", "SIH", "COSTVALUE", "SELLVALUE") FROM stdin;
    public          rishi    false    219   �i       g          0    16513    selling 
   TABLE DATA           1   COPY public.selling (itemcode, sell) FROM stdin;
    public          rishi    false    235   �i       X          0    16422    selling_import 
   TABLE DATA           4   COPY public.selling_import (code, sell) FROM stdin;
    public          rishi    false    220   	j       f          0    16506    sih_current 
   TABLE DATA           H   COPY public.sih_current (itemcode, "desc", sih, cost, sell) FROM stdin;
    public          rishi    false    234   &j       a          0    16473    sih_history 
   TABLE DATA           R   COPY public.sih_history (datetime, itemcode, "desc", sih, cost, sell) FROM stdin;
    public          rishi    false    229   Cj       c          0    16487    sih_history_desc 
   TABLE DATA           F   COPY public.sih_history_desc (datetime, itemcode, "desc") FROM stdin;
    public          rishi    false    231   `j       ^          0    16454    tentative_revenue 
   TABLE DATA           d   COPY public.tentative_revenue (itemcode, daydate, timehour, quantity, sumsell, sumcost) FROM stdin;
    public          rishi    false    226   }j       ]          0    16449    tentative_revenue_import 
   TABLE DATA           i   COPY public.tentative_revenue_import (productcode, daydate, timehour, qty, sumsell, sumcost) FROM stdin;
    public          rishi    false    225   �j       n          0    16554    vendors 
   TABLE DATA           9   COPY public.vendors (vendorcode, vendorname) FROM stdin;
    public          rishi    false    242   �j       m          0    16549    vendors_import 
   TABLE DATA           @   COPY public.vendors_import (vendorcode, vendorname) FROM stdin;
    public          rishi    false    241   �j       �           2606    16538    cost cost_pkey 
   CONSTRAINT     [   ALTER TABLE ONLY public.cost
    ADD CONSTRAINT cost_pkey PRIMARY KEY (itemcode, daydate);
 8   ALTER TABLE ONLY public.cost DROP CONSTRAINT cost_pkey;
       public            rishi    false    238    238            �           2606    16524 $   cost_purchase_2 cost_purchase_2_pkey 
   CONSTRAINT     h   ALTER TABLE ONLY public.cost_purchase_2
    ADD CONSTRAINT cost_purchase_2_pkey PRIMARY KEY (itemcode);
 N   ALTER TABLE ONLY public.cost_purchase_2 DROP CONSTRAINT cost_purchase_2_pkey;
       public            rishi    false    236            �           2606    16531     cost_purchase cost_purchase_pkey 
   CONSTRAINT     d   ALTER TABLE ONLY public.cost_purchase
    ADD CONSTRAINT cost_purchase_pkey PRIMARY KEY (itemcode);
 J   ALTER TABLE ONLY public.cost_purchase DROP CONSTRAINT cost_purchase_pkey;
       public            rishi    false    237            �           2606    16472 2   full_inventory_current full_inventory_current_pkey 
   CONSTRAINT     v   ALTER TABLE ONLY public.full_inventory_current
    ADD CONSTRAINT full_inventory_current_pkey PRIMARY KEY (itemcode);
 \   ALTER TABLE ONLY public.full_inventory_current DROP CONSTRAINT full_inventory_current_pkey;
       public            rishi    false    228            �           2606    16486 2   full_inventory_history full_inventory_history_pkey 
   CONSTRAINT     �   ALTER TABLE ONLY public.full_inventory_history
    ADD CONSTRAINT full_inventory_history_pkey PRIMARY KEY (datetime, itemcode);
 \   ALTER TABLE ONLY public.full_inventory_history DROP CONSTRAINT full_inventory_history_pkey;
       public            rishi    false    230    230            �           2606    16505    hourly hourly_pkey 
   CONSTRAINT     i   ALTER TABLE ONLY public.hourly
    ADD CONSTRAINT hourly_pkey PRIMARY KEY (itemcode, daydate, timehour);
 <   ALTER TABLE ONLY public.hourly DROP CONSTRAINT hourly_pkey;
       public            rishi    false    233    233    233            �           2606    16438    inventory inventory_pkey 
   CONSTRAINT     \   ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_pkey PRIMARY KEY (itemcode);
 B   ALTER TABLE ONLY public.inventory DROP CONSTRAINT inventory_pkey;
       public            rishi    false    222            �           2606    16565    prod_list prod_list_pkey 
   CONSTRAINT     ]   ALTER TABLE ONLY public.prod_list
    ADD CONSTRAINT prod_list_pkey PRIMARY KEY (dest, src);
 B   ALTER TABLE ONLY public.prod_list DROP CONSTRAINT prod_list_pkey;
       public            rishi    false    243    243            �           2606    16543 2   product_vendors_import product_vendors_import_pkey 
   CONSTRAINT     �   ALTER TABLE ONLY public.product_vendors_import
    ADD CONSTRAINT product_vendors_import_pkey PRIMARY KEY (itemcode, vendorcode);
 \   ALTER TABLE ONLY public.product_vendors_import DROP CONSTRAINT product_vendors_import_pkey;
       public            rishi    false    239    239            �           2606    16548 $   product_vendors product_vendors_pkey 
   CONSTRAINT     t   ALTER TABLE ONLY public.product_vendors
    ADD CONSTRAINT product_vendors_pkey PRIMARY KEY (itemcode, vendorcode);
 N   ALTER TABLE ONLY public.product_vendors DROP CONSTRAINT product_vendors_pkey;
       public            rishi    false    240    240            �           2606    16517    selling selling_pkey 
   CONSTRAINT     X   ALTER TABLE ONLY public.selling
    ADD CONSTRAINT selling_pkey PRIMARY KEY (itemcode);
 >   ALTER TABLE ONLY public.selling DROP CONSTRAINT selling_pkey;
       public            rishi    false    235            �           2606    16512    sih_current sih_current_pkey 
   CONSTRAINT     `   ALTER TABLE ONLY public.sih_current
    ADD CONSTRAINT sih_current_pkey PRIMARY KEY (itemcode);
 F   ALTER TABLE ONLY public.sih_current DROP CONSTRAINT sih_current_pkey;
       public            rishi    false    234            �           2606    16479    sih_history sih_history_pkey 
   CONSTRAINT     j   ALTER TABLE ONLY public.sih_history
    ADD CONSTRAINT sih_history_pkey PRIMARY KEY (datetime, itemcode);
 F   ALTER TABLE ONLY public.sih_history DROP CONSTRAINT sih_history_pkey;
       public            rishi    false    229    229            �           2606    16462 (   tentative_revenue tentative_revenue_pkey 
   CONSTRAINT        ALTER TABLE ONLY public.tentative_revenue
    ADD CONSTRAINT tentative_revenue_pkey PRIMARY KEY (itemcode, daydate, timehour);
 R   ALTER TABLE ONLY public.tentative_revenue DROP CONSTRAINT tentative_revenue_pkey;
       public            rishi    false    226    226    226            �           2606    16553 "   vendors_import vendors_import_pkey 
   CONSTRAINT     h   ALTER TABLE ONLY public.vendors_import
    ADD CONSTRAINT vendors_import_pkey PRIMARY KEY (vendorcode);
 L   ALTER TABLE ONLY public.vendors_import DROP CONSTRAINT vendors_import_pkey;
       public            rishi    false    241            �           2606    16560    vendors vendors_pkey 
   CONSTRAINT     Z   ALTER TABLE ONLY public.vendors
    ADD CONSTRAINT vendors_pkey PRIMARY KEY (vendorcode);
 >   ALTER TABLE ONLY public.vendors DROP CONSTRAINT vendors_pkey;
       public            rishi    false    242            �           1259    16577    cost_purchase_covering    INDEX     g   CREATE INDEX cost_purchase_covering ON public.cost_purchase USING btree (itemcode, runno, date, cost);
 *   DROP INDEX public.cost_purchase_covering;
       public            rishi    false    237    237    237    237            �           1259    16576    cost_purchase_itemcode    INDEX     T   CREATE INDEX cost_purchase_itemcode ON public.cost_purchase USING btree (itemcode);
 *   DROP INDEX public.cost_purchase_itemcode;
       public            rishi    false    237            �           1259    16567    full_inventory_current_covering    INDEX     r   CREATE INDEX full_inventory_current_covering ON public.full_inventory_current USING btree (itemcode, sell, cost);
 3   DROP INDEX public.full_inventory_current_covering;
       public            rishi    false    228    228    228            �           1259    16574    hourly_covering    INDEX     u   CREATE INDEX hourly_covering ON public.hourly USING btree (itemcode, daydate, timehour, quantity, sumsell, sumcost);
 #   DROP INDEX public.hourly_covering;
       public            rishi    false    233    233    233    233    233    233            �           1259    16571    hourly_index_for_trends    INDEX     a   CREATE INDEX hourly_index_for_trends ON public.hourly USING btree (itemcode, daydate, quantity);
 +   DROP INDEX public.hourly_index_for_trends;
       public            rishi    false    233    233    233            �           1259    16572 !   hourly_index_for_trends_replaceme    INDEX     k   CREATE INDEX hourly_index_for_trends_replaceme ON public.hourly USING btree (daydate, itemcode, quantity);
 5   DROP INDEX public.hourly_index_for_trends_replaceme;
       public            rishi    false    233    233    233            �           1259    16573    hourly_sales_averages    INDEX     �   CREATE INDEX hourly_sales_averages ON public.hourly USING btree (itemcode, daydate, ((sumsell / (quantity)::double precision)), ((sumcost / (quantity)::double precision)));
 )   DROP INDEX public.hourly_sales_averages;
       public            rishi    false    233    233    233    233    233            �           1259    16575    hourly_sold_prices    INDEX     �   CREATE INDEX hourly_sold_prices ON public.hourly USING btree (itemcode, daydate, ((sumsell / (quantity)::double precision)), ((sumcost / (quantity)::double precision)));
 &   DROP INDEX public.hourly_sold_prices;
       public            rishi    false    233    233    233    233    233            �           1259    16569    product_id_with_date_and_hour    INDEX     g   CREATE INDEX product_id_with_date_and_hour ON public.hourly USING btree (itemcode, daydate, timehour);
 1   DROP INDEX public.product_id_with_date_and_hour;
       public            rishi    false    233    233    233            �           1259    16578    selling_covering    INDEX     N   CREATE INDEX selling_covering ON public.selling USING btree (itemcode, sell);
 $   DROP INDEX public.selling_covering;
       public            rishi    false    235    235            �           1259    16568    sih_covering    INDEX     a   CREATE INDEX sih_covering ON public.sih_current USING btree (itemcode, "desc", sih, cost, sell);
     DROP INDEX public.sih_covering;
       public            rishi    false    234    234    234    234    234            �           1259    16566    tentative_revenue_everything    INDEX     �   CREATE INDEX tentative_revenue_everything ON public.tentative_revenue USING btree (itemcode, daydate, timehour, sumsell, sumcost);
 0   DROP INDEX public.tentative_revenue_everything;
       public            rishi    false    226    226    226    226    226            �           1259    16570    updated_dates    INDEX     C   CREATE INDEX updated_dates ON public.hourly USING btree (daydate);
 !   DROP INDEX public.updated_dates;
       public            rishi    false    233            S      x������ � �      j      x������ � �      U      x������ � �      i      x������ � �      h      x������ � �      `      x������ � �      _      x������ � �      b      x������ � �      e      x������ � �      T      x������ � �      \      x������ � �      Z      x������ � �      [      x������ � �      o      x������ � �      d      x������ � �      Y      x������ � �      l      x������ � �      k      x������ � �      V      x������ � �      W      x������ � �      g      x������ � �      X      x������ � �      f      x������ � �      a      x������ � �      c      x������ � �      ^      x������ � �      ]      x������ � �      n      x������ � �      m      x������ � �     