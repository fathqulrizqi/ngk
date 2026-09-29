$(document).ready(function () {

    $(".tblQCCList").tablesorter({
        theme: "bootstrap",
        widthFixed: true,

        // widget code contained in the jquery.tablesorter.widgets.js file
        // use the zebra stripe widget if you plan on hiding any rows (filter widget)
        // the uitheme widget is NOT REQUIRED!
        widgets: ["filter", "columns", "stickyHeaders"],
        widgetOptions: {
            // class names added to columns when sorted
            columns: ["primary", "secondary", "tertiary"],
            // extra css class name (string or array) added to the filter element (input or select)
            filter_cssFilter: [
                'form-control',
                'form-control',
                'form-control', // select needs custom class names :(
                'form-control',
                'form-control',
                'form-control',
                'form-control',
                'form-control',
                'form-control',
                'form-control',
                'form-control'
            ]
        }
    }).tablesorterPager({
        cssGoto: '.pagenum',
        container: $(".ts-pager"),
        output: '{startRow} to {endRow} ({totalRows})',
        size: 10
    });


    $(".btnDelete").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(this).data("id");
        if (confirm("Are you sure want to delete this data ?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/QCC/Data/deleteList",
                data: { iID: currID },
                success: function (data) {
                    currTR.remove();
                    $("#tblQCCList").trigger("update");
                }, error: function () {
                    $("#tblQCCList").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
});