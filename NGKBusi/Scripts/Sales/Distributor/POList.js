$(document).ready(function () {
    $('#tblPOList').DataTable({
        "ajax": {
            "url": "/Sales/Distributor/GetPOHeaders",
            "type": "GET",
            "datatype": "json",
            "dataSrc": ""
        },
        "columns": [
            { "data": "Niterra_PO" },
            { "data": "Cust_Name" },
            { "data": "Cust_Code" },
            { 
                "data": "Date",
                "render": function (data) {
                    if (!data) return "";
                    var d = new Date(data);
                    return d.toLocaleDateString('en-GB');
                }
            },
            { "data": "Distro_PO" },
            { "data": "Niterra_SO" }
        ]
    });
});