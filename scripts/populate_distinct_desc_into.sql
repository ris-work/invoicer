INSERT INTO imported_dummy.sih SELECT any_value(itemcode), "desc", 
	any_value(sih), any_value(cost), 
	any_value(sell)
	FROM imported_dummy.sih_current GROUP BY "desc";