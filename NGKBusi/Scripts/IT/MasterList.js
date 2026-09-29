$(document).ready(function () {
    if ($("#mlReportTable").length) {
        $("#mlReportTable").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'MasterList.csv',
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }

    $('.btnDownload').on("click", function () {
        $("#mlReportTable").trigger('outputTable');
    });

    $("#mlModal #txtIP").inputmask("1\\92.168.9.9[99]", { keepStatic: true, clearIncomplete: true });
    $("#mlModal #txtMacAddress").inputmask("**-**-**-**-**-**", { keepStatic: true, clearIncomplete: true, casing: "upper" });
    $("#mlModal #txtAnydesk").inputmask("999 999 999", { keepStatic: true, clearIncomplete: true });    
    $("#mlModal #selNIK").change(function () {
        var divName = $(this).find(':selected').data("div") || "-";
        var deptName = $(this).find(':selected').data("dept") || "-";
        var sectName = $(this).find(':selected').data("sect") || "-";
        var subSectName = $(this).find(':selected').data("subsection") || "-";
        $("#mlModal #spanDiv").text(divName);
        $("#mlModal #spanDept").text(deptName);
        $("#mlModal #spanSect").text(sectName);
        $("#mlModal #spanSubSect").text(subSectName);
    });

    $('.btnAdd').on("click", function () {
        $("#mlForm").attr("action", "/NGKBusi/IT/MasterList/insertList");
        $("#mlForm")[0].reset();
        $("#selNIK").change();
        $("#mlModal").modal("show");
    });

    $('.btnEdit').click(function () {
        var currTR = $(this).closest("tr");
        var isUsed = currTR.find(".tdIsUsed").text() == "Used" ? 1 : 0;
        $("#selNIK").val(currTR.data("nik")).change();
        $("#hfDataID").val(currTR.data("id"));
        $("#txtAssetNo").val(currTR.find(".tdAssetNo").text());
        $("#txtBrand").val(currTR.find(".tdBrand").text());
        $("#txtModel").val(currTR.find(".tdModel").text());
        $("#txtComputerName").val(currTR.find(".tdComputerName").text());
        $("#txtProcessor").val(currTR.find(".tdProcessor").text());
        $("#selMSOffice").val(currTR.find(".tdMSOffice").text());
        $("#txtMSOfficeUser").val(currTR.find(".tdMSOfficeUser").text());
        $("#selRAM").val(currTR.find(".tdRAM").text());
        $("#selHDD").val(currTR.find(".tdHDD").text());
        $("#selOS").val(currTR.find(".tdOS").text());
        $("#txtIP").val(currTR.find(".tdIP").text());
        $("#txtMacAddress").val(currTR.find(".tdMacAddress").text());
        $("#txtAnydesk").val(currTR.find(".tdAnydesk").text());
        $("#selPurchase").val(currTR.find("tdPurchase").text());
        $("#selMonth").val(currTR.find(".tdMonth").text());
        $("#selType").val(currTR.find(".tdType").text());
        $("#txtRemark").val(currTR.find(".tdRemark").text());
        $("input[name='iUsed'][value='" + isUsed + "']").prop("checked", true);
        $("#mlForm").attr("action","/NGKBusi/IT/MasterList/editList");
        $("#mlModal").modal("show");
    });
    $(".btnDelete").click(function () {
        var currTR = $(this).closest("tr");
        var currID = currTR.data("id");
        if (confirm('Hapus data ini ?')) {
            $.ajax({
                type:"POST",
                url: "/NGKBusi/IT/MasterList/deleteList",
                data: {
                    currDataID : currID
                },
                success: function (data) {
                    currTR.remove();
                }
            });
        }
    });
});