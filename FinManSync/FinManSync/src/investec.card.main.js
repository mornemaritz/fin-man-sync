// This function runs during the card transaction authorization flow.
// It has a limited execution time, so keep any code short-running.
const beforeTransaction = async () => {
    // console.log(authorization);
    return true;
};

// This function runs after an approved transaction.
const afterTransaction = async (transaction) => {

    var amount = transaction.centsAmount;
    var originalCurrencyAmount = '';

    if (transaction.currencyCode != "zar")
    {
        originalCurrencyAmount = `(${transaction.centsAmount / 100}${transaction.currencyCode.toUpperCase()})`;

        const conversionUrl = `https://api.fxratesapi.com/convert?api_key=${process.env.FX_RATES_API_KEY}&from=${transaction.currencyCode.toUpperCase()}&to=ZAR&amount=${transaction.centsAmount / 100}`;
        try {
            const response = await fetch(conversionUrl);
            if (!response.ok) {
                console.error(`Currency Converter Response status: ${response.status}`);
            }

            const json = await response.json();
            console.log(json);
            amount = json.result * 100;
        } catch (error) {
            console.error(error.message);
        }        
    }

    const ynabTransactionUrl = `https://api.ynab.com/v1/budgets/${process.env.YNAB_BUDGET_ID}/transactions`;
    const cardNumber = transaction.card.display ? transaction.card.display.slice(-5) : 'card unknown';
    try {
        const response = await fetch(ynabTransactionUrl, {
            method: 'POST',
             headers: {
                'accept': 'application/json',
                'Authorization': `Bearer ${process.env.YNAB_BEARER_TOKEN}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                'transaction': {
                    'account_id': "`${process.env.YNAB_ACCOUNT_ID}`",
                    'date': new Date().toJSON().slice(0,10),
                    'amount' : (parseInt(amount) * 10) * -1,
                    "payee_name": transaction.merchant.name,
                    "memo": `real-time ${originalCurrencyAmount} ${cardNumber} `
                }
            })
        });
        if (!response.ok) {
            var body = await response.json();
            throw new Error(`Unable to post transaction to YNAB: ${response.status} - ${JSON.stringify(body)}`);
        }

    } catch (error) {
        console.error(error.message);
    }     
};

// This function runs after a declined transaction
const afterDecline  = async () => {
    // console.log(transaction);
};
