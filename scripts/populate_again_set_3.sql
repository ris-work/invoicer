INSERT INTO
	PUBLIC.CATALOGUE (
		ITEMCODE,
		DESCRIPTION,
		ACTIVE,
		CREATED_ON,
		DESCRIPTION_POS,
		DESCRIPTION_WEB,
		DESCRIPTIONS_OTHER_LANGUAGES,
		DEFAULT_VAT_CATEGORY,
		VAT_DEPENDS_ON_USER,
		VAT_CATEGORY_ADJUSTABLE,
		PRICE_MANUAL,
		ENFORCE_ABOVE_COST,
		ACTIVE_WEB
	)
SELECT 

		ITEMCODE +200000,
		DESCRIPTION || 'COLOURED',  
		ACTIVE,
		CREATED_ON,
		DESCRIPTION_POS,
		DESCRIPTION_WEB,
		DESCRIPTIONS_OTHER_LANGUAGES,
		DEFAULT_VAT_CATEGORY,
		VAT_DEPENDS_ON_USER,
		VAT_CATEGORY_ADJUSTABLE,
		PRICE_MANUAL,
		ENFORCE_ABOVE_COST,
		ACTIVE_WEB

	FROM PUBLIC.CATALOGUE;