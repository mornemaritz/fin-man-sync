function emailFunction() {
  // Email Config
  var recipientsTO = ""; // email address e.g. "test@test.com, test2@test.com"
  // var recipientsTO = "mornemaritz@gmail.com";
  var emailSubject = `Daily Budget Report for ${new Date().toLocaleString('default', { month: 'long' })}`;

  // Private Keys
  accessToken = ""; //Add your YNAB developer access token
  budgetID = ""; //Add your YNAB budget id

  // Get data from YNAB (specifically Categories & Spending)
  // var categorySpendingData = get_ynab_category_spending(accessToken,budgetID)
  var categorySpendingData = get_ynab_financial_summary(accessToken,budgetID);
  // var transactionData = get_ynab_transactions(accessToken,budgetID);

  // Format Data for email form
  // var formattedCategorySpeningData = formatCategorySpendingData(categorySpendingData);
  // var formattedTransactionData = formatTransactionData(transactionData);

  // Add to email template and subsitute content
  // var htmlOutput = HtmlService.createHtmlOutputFromFile('email'); //name of file is email.html
  // var htmlEmailBody = htmlOutput.getContent()
  // htmlEmailBody = htmlEmailBody.replace("%tablecontent", formattedCategorySpeningData);
  // htmlEmailBody = htmlEmailBody.replace("%transactions", formattedTransactionData);

  var htmlOutput = HtmlService.createHtmlOutputFromFile('shortEmail'); //name of file is email.html
  var htmlEmailBody = htmlOutput.getContent()
  htmlEmailBody = htmlEmailBody.replace("%remaining", categorySpendingData[0][3]);

  // Send Email
  sendEmail(emailSubject, htmlEmailBody, recipientsTO);
}

function fetch_ynab_data(accessToken, path){
  const url = "https://api.youneedabudget.com/v1/" + path;
  const options = {
    "headers": {
      "Authorization": "Bearer " + accessToken
    }
  };
  const response = UrlFetchApp.fetch(url, options);
  const data = JSON.parse(response.getContentText()).data;
  return data;
}

function formatCurrency(symbol, amount) {
  if (!isNaN(parseFloat(amount)) && isFinite(amount)) {

    var aDigits = amount.toFixed(2).split(".");
    
    // The following splits thousands with commas
    aDigits[0] = aDigits[0]
      .split("")
      .reverse()
      .join("")
      .replace(/(\d{3})(?=\d)/g,"$1,")
      .split("")
      .reverse()
      .join("");

    return symbol + aDigits.join(".");
  }
  return amount
}

function get_ynab_financial_summary(accessToken, budgetID) {
  const categorySpending = fetch_ynab_data(accessToken, `budgets/${budgetID}/categories`);

  var taggedCategorySpending = 
    categorySpending.category_groups
    .flatMap(group => group.categories || [])
    .filter(category => category.note && category.note.includes("group="))
    .reduce((acc, category) => {
      const tags = parseCategoryNote(category.note);
      for (const [key, value] of Object.entries(tags)) {
        if (!acc[value]) {
          acc[value] = { budgeted: 0, activity: 0, balance: 0 };
        }
        acc[value].budgeted += category.budgeted / 1000.0;
        acc[value].activity += category.activity / 1000.0;
        acc[value].balance += category.balance / 1000.0;
      }
      return acc;
    }
    , {});

  // console.log(JSON.stringify(taggedCategorySpending));

  const summary = [];
  // for (const [group, data] of Object.entries(taggedCategorySpending)) {
  //   summary.push([group, data.budgeted, data.activity, data.balance]);
  // }

  summary.push(['day-to-day', taggedCategorySpending['day-to-day'].budgeted, taggedCategorySpending['day-to-day'].activity, taggedCategorySpending['day-to-day'].balance]);

  return summary;
}

function fetch_categories_by_tag(accessToken, tagKey, tagValue, budgetID) {
  const categories = fetch_ynab_data(accessToken, `budgets/${budgetID}/categories`).category_groups;

  const taggedCategories = [];
  categories.forEach(group => {
    group.categories.forEach(category => {
      if (category.note && category.note.includes(`${tagKey}=${tagValue}`)) {
        taggedCategories.push(category.id);
      }
    });
  });

  return taggedCategories;
}

function get_ynab_category_spending(accessToken, budgetID) {

  const groups = fetch_ynab_data(accessToken, "budgets/" + budgetID + "/categories").category_groups;

  //const columns = ["Name", "Budgeted", "Activity", "Balance"];
  const rows = [];

  for (var group_idx = 0; group_idx < groups.length; group_idx++) {
    // Add the group
    var group = groups[group_idx];
    // Skip internal and hidden categories
    if (['Internal Master Category', 'Hidden Categories'].indexOf(group.name) >= 0) continue;
    rows.push([group.name]);

    // Add the categories
    for (var category_idx = 0; category_idx < group.categories.length; category_idx++) {
      var category = group.categories[category_idx];

      var name = "      " + category.name; // Indent categories a bit so they are offset from groups
      // Calculate currency amounts from mulliunits
      var budgeted = category.budgeted / 1000.0;
      var activity = category.activity / 1000.0;
      var balance = category.balance / 1000.0;

      rows.push([name, budgeted, activity, balance]);
    }
  }
  return rows;
};

function parseCategoryNote(note) {
  if (!note) return {}; // Return empty object if null or empty

  return note.split(",")
    .map(pair => pair.trim())
    .filter(pair => pair.includes("="))
    .reduce((acc, pair) => {
      const [key, value] = pair.split("=").map(s => s.trim());
      acc[key] = value;
      return acc;
    }, {});
}

// Example usage:
// let note = "tag1=value1, tag2=value2";
// let tags = parseCategoryNote(note);
// tags["tag1"] === "value1"

function get_ynab_transactions(accessToken,budgetID) {

  // Roll back date by 7 days
  var day = new Date();
  day.setDate(day.getDate() - 7);
  day = day.toISOString().substr(0,10);

  const rawTransactions = fetch_ynab_data(accessToken, "budgets/" + budgetID + "/transactions?since_date=" + day).transactions;
  const dataToDayTaggedCategories = fetch_categories_by_tag(accessToken, "group", "day-to-day", budgetID);

  // console.log(JSON.stringify(dataToDayTaggedCategories));

  var transactions = []

  rawTransactions.forEach(function (transaction) {

    var day = transaction.date;
    var amount = transaction.amount / 1000.0;
    var account_name = transaction.account_name;
    var payee_name = transaction.payee_name;
   
    var category = transaction.category_name || " "; // Default to empty string if null
    var memo = transaction.memo || " "; // Default to empty string if null

    // Check if the transaction's category is tagged with "day-to-day"
    if (dataToDayTaggedCategories.includes(transaction.category_id)) {
      category = `${category} (day-to-day)`;
    }

    transactions.push([day, amount, account_name, payee_name, category, memo]);
  });
  return transactions;
};

function formatTransactionData(rows){
  var tableMiddle = '';
  var zebraStripe = true // required for zebra striped rows

  rows.forEach(function (data) {
    data[1] = formatCurrency("R", data[1]);
    if (zebraStripe) {
      tableMiddle = tableMiddle + '<tr>'+
      '<td scope="row" class="zebra" align="left" valign="top">' + data[0] + '</td>' +
      '<td scope="row" class="zebra" align="left" valign="top">' + data[1] + '</td>' +
      '<td scope="row" class="zebra" align="left" valign="top">' + data[2] + '</td>' +
      '<td scope="row" class="zebra" align="left" valign="top">' + data[3] + '</td>' +
      '<td scope="row" class="zebra" align="left" valign="top">' + data[4] + '</td>' +
      '<td scope="row" class="zebra" align="left" valign="top">' + data[5] + '</td></tr>';
      zebraStripe = false;
    }
    else {
      tableMiddle = tableMiddle + '<tr>'+
      '<td>' + data[0] + '</td>' +
      '<td>' + data[1] + '</td>' +
      '<td>' + data[2] + '</td>' +
      '<td>' + data[3] + '</td>' +
      '<td>' + data[4] + '</td>' +
      '<td>' + data[5] + '</td></tr>';
      zebraStripe = true;
    }

  });
  return tableMiddle;
}


function formatCategorySpendingData(data) {
  var tableMiddle = ''
  var zebraStripe = true // required for zebra striped rows

  data.forEach(function (row) {
    var name = row[0];
    var budget = row[1];
    var activity = row[2];
    var balance = row[3];

    if (name != '') {
      // check if table header
      if ((budget == null)) {
        tableMiddle = '<tr class="sectionHeader">' + tableMiddle + '<td class="sectionHeader" align="left" valign="top">'+ name + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + "" + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + "" + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + "" + '</td>'
      }

      // check if section header
      else if ((budget === "") && (activity ===  "") && (balance ===  "")) {
        tableMiddle = '<tr class="sectionHeader">' + tableMiddle + '<td class="sectionHeader" align="left" valign="top">'+ name + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + formatCurrency("R", budget) + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + formatCurrency("R", activity) + '</td>'
        tableMiddle = tableMiddle + '<td class="sectionHeader">' + formatCurrency("R", balance) + '</td>'
      }

      else {
        if (zebraStripe) {
          tableMiddle = '<tr>' + tableMiddle + '<td scope="row" class="zebra" align="left" valign="top">' + name + '</td>'
          tableMiddle = tableMiddle + '<td class="zebra">' + formatCurrency("R", budget) + '</td>'
          tableMiddle = tableMiddle + '<td class="zebra">' + formatCurrency("R", activity) + '</td>'
          if (!isNaN(parseFloat(balance)) && isFinite(balance) && balance < 0) {
            tableMiddle = tableMiddle + '<td class="zebra" valign="top" style="color:red">' + formatCurrency("R", balance) + '</td>'
          }
          else {
            tableMiddle = tableMiddle + '<td class="zebra" valign="top" style="color:#035c1f">' + formatCurrency("R", balance) + '</td>'
          }
          zebraStripe = false
        }
        else {
          zebraStripe = true
          tableMiddle = '<tr> ' + tableMiddle + '<td align="left" valign="top">' + name + '</td>'
          tableMiddle = tableMiddle + '<td valign="top">' + formatCurrency("R", budget) + '</td>'
          tableMiddle = tableMiddle + '<td valign="top">' + formatCurrency("R", activity) + '</td>'
          if (!isNaN(parseFloat(balance)) && isFinite(balance) && balance < 0) {
            tableMiddle = tableMiddle + '<td valign="top" style="color:red">' + formatCurrency("R", balance) + '</td>'
          }
          else {
            tableMiddle = tableMiddle + '<td valign="top" style="color:#035c1f">' + formatCurrency("R", balance) + '</td>'
          }
        }
      }

      tableMiddle = tableMiddle + '</tr>'
    }
  });
  return (tableMiddle)
}


function sendEmail(subject, body, recipients) {
  MailApp.sendEmail({
    to: recipients,
    subject: subject,
    htmlBody: body
  });
}