$(document).ready(function () {

    if ($("#tblLCRate").length) {
        $("#tblLCRate").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'reflow', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'Labor_Cost_Master.csv',
                filter_saveFilters: true
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }

    $("#btnLCRateAdd").click(function () {
        var dt = new Date();
        var beginDate = 2018;
        var endDate = parseInt(dt.getFullYear(), 10) + 4;
        $("#formLCRate")[0].reset();
        $("#selLCPeriod option[value!='']").remove();
        for (var i = beginDate; i <= endDate; i++) {
            $("#selLCPeriod").append("<option value='" + i + "'>" + i + "</option>");
        }
        $("#selLCPeriod").prop("required", true);
        $("#selLCPosition,#selLCPeriod").show();
        $("#lblLCPosition,#lblLCPeriod").hide();
        $("#formLCRate").attr("action", "/NGKBusi/FA/LaborCost/RateAdd");
    });

    $(".btnLCRateEdit").click(function () {
        var currCTR = $(this);
        var currID = $(this).data("id");
        var dt = new Date();
        var beginDate = 2018;
        var endDate = parseInt(dt.getFullYear(), 10) + 4;
        var period = currCTR.closest("tr").find("td:eq(0)").text();
        var bpjs_kesehatan = currCTR.closest("tr").find("td:eq(1)").text().replace("%","");
        var bpjs_kesehatan_maks = currCTR.closest("tr").find("td:eq(2)").text().replace(/\,/g, "").replace(/\./g, "");
        var bpjs_jkk_jk = currCTR.closest("tr").find("td:eq(3)").text().replace("%", "");
        var jshk = currCTR.closest("tr").find("td:eq(4)").text().replace("%", "");
        var tax_allowance = currCTR.closest("tr").find("td:eq(5)").text().replace("%", "");
        var bpjs_jht = currCTR.closest("tr").find("td:eq(6)").text().replace("%", "");
        var bpjs_jp = currCTR.closest("tr").find("td:eq(7)").text().replace("%", "");
        var bpjs_jp_max = currCTR.closest("tr").find("td:eq(8)").text().replace(/\,/g, "").replace(/\./g, "");
        var thr = currCTR.closest("tr").find("td:eq(9)").text().replace("%", "");
        var alpha = currCTR.closest("tr").find("td:eq(10)").text().replace("%", "");
        var tat = currCTR.closest("tr").find("td:eq(11)").text().replace("%", "");
        var ptkp = currCTR.closest("tr").find("td:eq(12)").text().replace(/\,/g, "").replace(/\./g, "");
        var promosi = currCTR.closest("tr").find("td:eq(13)").text().replace("%", "");
        var asst_manager_overtime = currCTR.closest("tr").find("td:eq(14)").text().replace(/\,/g, "").replace(/\./g, "");
        $("#selLCPeriod option[value!='']").remove();
        for (var i = beginDate; i <= endDate; i++) {
            $("#selLCPeriod").append("<option value='" + i + "'>" + i + "</option>");
        }
        $("#selLCPeriod").val(period);
        $("#hfLCID").val(currID);
        $("#txtLCBPJSKes").val(bpjs_kesehatan);
        $("#txtLCBPJSKesMax").val(bpjs_kesehatan_maks);
        $("#txtLCBPJSJKKJK").val(bpjs_jkk_jk);
        $("#txtLCJSHK").val(jshk);
        $("#txtLCTaxAllowance").val(tax_allowance);
        $("#txtLCBPJSJHT").val(bpjs_jht);
        $("#txtLCBPJSJP").val(bpjs_jp);
        $("#txtLCBPJSJPMax").val(bpjs_jp_max);
        $("#txtLCTHR").val(thr);
        $("#txtLCALPHA").val(alpha);
        $("#txtLCTAT").val(tat);
        $("#txtLCPTKP").val(ptkp);
        $("#txtLCPromosi").val(promosi);
        $("#txtLCAsstManOvertime").val(asst_manager_overtime);

        $("#selLCPeriod").prop("required", true);
        $("#selLCPosition,#selLCPeriod").show();
        $("#lblLCPosition,#lblLCPeriod").hide();
        $("#formLCRate").attr("action", "/NGKBusi/FA/LaborCost/RateEdit");
        $("#LCRateModal").modal();
        
    });
    
    $(".btnLCRateDelete").click(function () {
        if (confirm("Are you sure want to delete this data ?")) {
            var currTR = $(this).closest("tr");
            var id = currTR.data("id");
            currTR.css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/LaborCost/RateDelete",
                data: {
                    iID: id
                },
                tryCount: 0,
                tryLimit: 3,
                success: function (data) {
                    currTR.remove();
                },
                error: function (textStatus) {
                    if (textStatus == "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    currTR.css("background-color", "initial");
                    alert("Error Occurred, Please Try Again.");
                }
            });
        }
    });
});