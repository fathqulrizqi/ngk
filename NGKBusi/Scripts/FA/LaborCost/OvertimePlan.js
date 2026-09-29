$(document).ready(function () {
    var period = parseInt($(".selLCOvertimePlanPeriod").val(),10);
    var cHeader = ['NIK', 'Name', 'Department', 'Position', 'Status', 'Category'];
    var cwidth = [80, 200, 200, 150, 150, 150];
    var currDate = new Date(period, 9, 1);
    var lastDate = new Date(period + 4, 3, 1);
    var mDiff = monthDiff(currDate, lastDate);
    for (var i = 0; i <= mDiff; i++) {
        cHeader.push(currDate.getFullYear() + "_" + ((currDate.getMonth() * 1) + 1));
        cwidth.push(100);
        currDate.setMonth(currDate.getMonth() + 1);
    }
    cHeader.push(" ");
    cHeader.push("Remark");
    cHeader.push("id");
    cwidth.push(100);
    cwidth.push(300);

    $("#btnUploadOTP").click(function () {
        $("#flLCUpload").click();
    });
    $("#flLCUpload").change(function () {
        if (window.FormData !== undefined) {
            $("#divOvertimePlan").LoadingOverlay("show");

            var ext = $('#flLCUpload').val().split('.').pop().toLowerCase();
            if ($.inArray(ext, ['xls']) > -1 || $.inArray(ext, ['xlsx']) > -1) {
                var fileUpload = $("#flLCUpload").get(0);
                var files = fileUpload.files;

                // Create FormData object  
                var fileData = new FormData();

                // Looping over all files and add it to FormData object  
                for (var i = 0; i < files.length; i++) {
                    fileData.append(files[i].name, files[i]);
                }
                fileData.append('iPeriod', period);

                $.ajax({
                    url: 'UploadOvertimePlan',
                    type: "POST",
                    contentType: false, // Not to set any content header  
                    processData: false, // Not to process data  
                    data: fileData,
                    tryCount: 0,
                    tryLimit: 3,
                    success: function (result) {
                        alert(result);
                        $("#divOvertimePlan").LoadingOverlay("hide", true);
                        location.reload();
                    },
                    error: function (err) {
                        alert(err.statusText);
                        $("#divOvertimePlan").LoadingOverlay("hide", true);
                    }
                });
            } else {
                alert("Please upload '.xls. or '.xlsx' file.");
                $("#divOvertimePlan").LoadingOverlay("hide", true);
            }
        } else {
            alert("FormData is not supported.");
        }
        $(this).val('');
    });

    $("#btnDownloadOTP").click(function () {
        $("#tblOTPList").trigger('outputTable');
    });
    $("#btnDownloadOTPSummary").click(function () {
        $("#tblLCOTPSectionSummary").trigger('outputTable');
    });

    $('#divOvertimePlan').jexcel({
        url: "/NGKBusi/FA/LaborCost/GetOvertimePlan?iOTPSignLevel=" + $("#hfOTPSignLevel").val() + "&iOTPPeriod=" + period,
        colHeaders: cHeader,
        colWidths: cwidth,
        tableOverflow: false,
        allowInsertRow: false,
        // Allow new rows
        allowManualInsertRow: false,
        // Allow new columns
        allowInsertColumn: false,
        // Allow new rows
        allowManualInsertColumn: false,
        // Allow row delete
        allowDeleteRow: false,
        // Allow column delete
        allowDeleteColumn: false,
        columns: [
            { readOnly: true }, { readOnly: true }, { readOnly: true }, { readOnly: true }, { readOnly: true }, { readOnly: true }
            , { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }
            , { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }
            , { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }
            , { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }, { type: "number" }]
    });

    $('#divOvertimePlan').jexcel("updateSettings", {
        cells: function (cell, col, row) {
            // If the column is number 4 or 5
            if (cell.text() == '-') {
                $(cell).addClass("readonly");
                $(cell).css("background-color", "lightGrey");
                $(cell).css("color", "lightGrey");
            } else if (cell.text() == '0') {
                $(cell).html("");
            } else if (cell.text() == 'Signed') {
                $(".jexcel tbody tr:eq(" + row + ") td").addClass("readonly");
                $(".jexcel tbody tr:eq(" + row + ") td").css("background-color", "lightGrey");
            }
            if (col == 48) {
                $(cell).addClass("readonly");
                $(cell).css("background-color", "lightGrey");
                $(cell).css("color", "lightGrey");
            } else if (col == 6) {
                $(cell).addClass("readonly");
                $(cell).css("background-color", "lightGrey");
                $(cell).css("color", "lightGrey");
                $(cell).html("");
            }
        }
    });

    $('#btnLCOvertimePlanSave').click(function () {
        $("#divOvertimePlan").LoadingOverlay("show");
        var currData = $('#divOvertimePlan').jexcel('getData');
        var currDataArray = [];
        for (var i = 0; i < currData.length; i++) {
            var dataArray = []
            for (var z = 0; z < currData[i].length; z++) {
                dataArray.push(currData[i][z]);
            }
            currDataArray.push(dataArray);
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/FA/LaborCost/SetOvertimePlan",
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify({
                'dataList': currDataArray,
                "iOTPPeriod": period
            }),
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                alert("Data Saved Successfully!");
                $("#divOvertimePlan").LoadingOverlay("hide", true);
                location.reload();
            },
            error: function (textStatus) {
                if (textStatus == "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#divOvertimePlan").LoadingOverlay("hide", true);
                alert("Error Occurred, Please Try Again.");
            }
        });
    });

    $(".cbLCOTPSign").change(function () {
        if ($(".cbLCOTPSign:checked").length > 0) {
            $("#btnLCOTPSign,#btnLCOTPReturn").prop("disabled", false);
        } else {
            $("#btnLCOTPSign,#btnLCOTPReturn").prop("disabled", true);
        }
    });

    $("#selLCOverTimePlanLevel,#selLCOvertimePlanPeriod").change(function () {
        $("#btnLCOverTimePlanLevel").click();
    });
    

    if ($("#tblOTPList").length) {
        $("#tblOTPList").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Overtime_Plan.csv',
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $("#tblOTPList .pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            // starting page of the pager (zero based index)
            page: 0,
            // Number of visible rows - default is 10
            size: 10,
        });
    }

    if ($("#tblLCOTPSectionSummary").length) {
        $("#tblLCOTPSectionSummary").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output', 'math'],
            cssChildRow: "tablesorter-childRow",
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Overtime_Plan_Summary.csv',
                filter_excludeFilter: {
                    2: 'range'
                },
                math_data: 'math', // data-math attribute
                math_ignore: [0, 1, 2, 3],
                math_none: 'N/A', // no matching math elements found (text added to cell)
                math_complete: function ($cell, wo, result, value, arry) {
                    var txt = '<span class="align-decimal">' +
                        (value === wo.math_none ? '' : '') +
                        result + '</span>';
                    if ($cell.attr('data-math') === 'all-sum') {
                        // when the "all-sum" is processed, add a count to the end
                        //return txt + ' (Sum of ' + arry.length + ' cells)';
                        return txt;
                    }
                    return txt;
                },
                math_completed: function (c) {
                    // c = table.config
                    // called after all math calculations have completed
                    console.log('math calculations complete', c.$table.find('[data-math="all-sum"]:first').text());
                },
                // see "Mask Examples" section
                math_mask: '#.#',
                math_prefix: '', // custom string added before the math_mask value (usually HTML)
                math_suffix: '', // custom string added after the math_mask value
                // event triggered on the table which makes the math widget update all data-math cells (default shown)
                math_event: 'recalculate',
                // math calculation priorities (default shown)... rows are first, then column above/below,
                // then entire column, and lastly "all" which is not included because it should always be last
                math_priority: ['row', 'above', 'below', 'col'],
                // set row filter to limit which table rows are included in the calculation (v2.25.0)
                // e.g. math_rowFilter : ':visible:not(.filtered)' (default behavior when math_rowFilter isn't set)
                // or math_rowFilter : ':visible'; default is an empty string
                math_rowFilter: '',

                scroller_height: 300,
                // scroll tbody to top after sorting
                scroller_upAfterSort: true,
                // pop table header into view while scrolling up the page
                scroller_jumpToHeader: true,
                // In tablesorter v2.19.0 the scroll bar width is auto-detected
                // add a value here to override the auto-detected setting
                scroller_barWidth: null,
                stickyHeaders_xScroll: "#divLCOTPSectionSummary"
            }
        }).tablesorterPager({
            container: $("#tblLCOTPSectionSummary .pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            // starting page of the pager (zero based index)
            page: 0,
            // Number of visible rows - default is 10
            size: 10
            })
            .on('filterStart', function () {
                $('#tblLCOTPSectionSummary').trigger('sortReset');
            })
            .on('tablesorter-initialized filterEnd', function () {
                $(".rowPermanent").each(function (e) {
                    var currRow = $(this);
                    var currSetion = "#rowContract" + currRow.data("section");
                    if ($(currSetion).length > 0) {
                        currRow.insertBefore(currSetion);
                    }
                });
            });
        $(".rowPermanent").each(function (e) {
            var currRow = $(this);
            var currSetion = "#rowContract" + currRow.data("section");
            if ($(currSetion).length > 0) {
                currRow.insertBefore(currSetion);
            }
        });
    }
    $("#selOTPFilterDept").change(function () {
        var currVal = "#" + $(this).val();
        var trCount = $("table.jexcel tbody tr").length;
        if (currVal == "#") {
            $("table.jexcel tbody tr").show();
        } else {
            $("table.jexcel tbody tr").hide();
            $("table.jexcel tbody tr").each(function () {
                var currSection = $(this).find("td:eq(3)").text();
                if (currVal.indexOf(currSection) > 0) {
                    $(this).show();
                }
            });
        }
    });
    $("#txtOTPFilterRemark").keyup(function () {
        var currVal = "#" + $(this).val().toLowerCase();
        var trCount = $("table.jexcel tbody tr").length;
        if (currVal == "#") {
            $("table.jexcel tbody tr").show();
        } else {
            $("table.jexcel tbody tr").hide();
            $("table.jexcel tbody tr").each(function () {
                var currRemark = $(this).find("td:eq(49)").text().toLowerCase();
                if (currVal.indexOf(currRemark) > 0) {
                    $(this).show();
                }
            });
        }
    });
});

function monthDiff(d1, d2) {
    var months;
    months = (d2.getFullYear() - d1.getFullYear()) * 12;
    months -= d1.getMonth() + 1;
    months += d2.getMonth();
    return months <= 0 ? 0 : months;
}