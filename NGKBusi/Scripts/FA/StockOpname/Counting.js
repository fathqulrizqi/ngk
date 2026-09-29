$(document).ready(function () {
    $(function () {
        $("#selWarehouse").change();
    });
    $("#selWarehouse").change(function () {
        var iWarehouse = $(this).val();
        $("#selProduct").LoadingOverlay("show");
        $("#hfItemProduct").val("");
        if (iWarehouse == "PD" || iWarehouse == "WH-KD" || iWarehouse == "HD" || iWarehouse == "TWH") {
            $(".divLot").show();
        } else {
            $(".divLot").hide();
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/FA/StockOpname/CountingGetProduct",
            dataType: "json",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iWarehouse: iWarehouse
            },
            success: function (data) {
                $("#selProduct option[value != '']").remove();
                $.each(data, function (i, v) {
                    var newOption = new Option;
                    newOption.value = v;
                    newOption.text = v;
                    $("#selProduct").append(newOption);
                });
                var currProduct = $("#hfItemProduct").val();
                if (currProduct.length > 0) {
                    $("#selProduct").val(currProduct);
                }
                $("#selProduct").LoadingOverlay("hide");
            }, error: function (textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                $("#selProduct").LoadingOverlay("hide");
                alert("Error Occurred, Please Try Again.");
            }
        });
    });

    $("#selProduct").change(function () {
        $("#txtDimension").val("");
        $("#txtItemID").val("");
        $("#hfItemID").val("");
        $("#hfItemName").val("");
        $("#hfItemProduct").val("");
    });

    $("#txtItemID").autocomplete({
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/StockOpname/CountingGetItemID",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iIgnore: $("#cbIgnoreProduct").is(":checked"),
                    iProduct: $("#selProduct").val(),
                    iItemID: $("#txtItemID").val(),
                    iWarehouse: $("#selWarehouse").val()
                },
                success: function (data) {
                    response(data);
                }, error: function (textStatus) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    alert("Error Occurred, Please Try Again.");
                }
            });
        },
        minLength: 2,
        select: function (event, ui) {
            if (ui.item.itemID.substring(0, 5) == "QIDTL") {
                $("#selWarehouse").val("TL").change();
            } else if (ui.item.itemID.substring(0, 5) == "QIDMP") {
                $("#selWarehouse").val("MP").change();
            }
            $("#txtDimension").val(ui.item.unit);
            $("#hfDimension").val(ui.item.unit);
            $("#selProduct").val(ui.item.product);
            $("#hfItemID").val(ui.item.itemID);
            $("#hfItemName").val(ui.item.itemName);
            $("#hfItemProduct").val(ui.item.product);
            $("#txtQty").focus();
        }
    });
    $("#txtItemID").autocomplete("option", "appendTo", "#formSOCounting");

    $("#txtLot").autocomplete({
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/StockOpname/CountingGetLot",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iLot: $("#txtLot").val(),
                    iItemID: $("#hfItemID").val(),
                    iWarehouse: $("#selWarehouse").val()
                },
                success: function (data) {
                    response(data);
                }, error: function (textStatus) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    alert("Error Occurred, Please Try Again.");
                }
            });
        },
        minLength: 2,
        select: function (event, ui) {
            $("#txtLot").val(ui.item.value);
        }
    });


    $("#btnSOSubmit").click(function (e) {
        var currBtn = $(this);
        if ($('#formSOCounting')[0].reportValidity()) {
            currBtn.LoadingOverlay("show");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/StockOpname/CountingAdd",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iWarehouse: $("#selWarehouse").val(),
                    iPeriod: $("#hfPeriod").val(),
                    iProduct: $("#selProduct").val(),
                    iLot: $("#txtLot").val(),
                    iItemID: $("#hfItemID").val(),
                    iItemName: $("#hfItemName").val(),
                    iUnit: $("#txtDimension").val(),
                    iQty: $("#txtQty").val(),
                    iDescription: $("#txtDescription").val()
                },
                success: function (data) {
                    currBtn.LoadingOverlay("hide");
                    alert("Data successfully added!");
                    $("#txtLot").val("DM");
                    $("#txtItemID").val("");
                    $("#txtDimension").val("");
                    $("#hfDimension").val("");
                    $("#hfItemID").val("");
                    $("#hfItemName").val("");
                    $("#txtQty").val("");
                    $("#txtDescription").val("");
                    $("#txtItemID").focus();
                }, error: function (textStatus) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    currBtn.LoadingOverlay("hide");
                    alert("Error Occurred, Please Try Again.");
                }
            });
        }
    });

    if ($("#tblCountingList").length) {
        $("#tblCountingList").tablesorter({
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

    $(".btnCLDelete").click(function () {
        var currCTR = $(this);
        var currTR = $(this).closest("tr");
        var currID = currCTR.data("id");
        if (confirm("Are you sure want to delete this data ?")) {
            currTR.LoadingOverlay("show");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/FA/StockOpname/CountingDelete",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iID: currID
                },
                success: function (data) {
                    currTR.LoadingOverlay("hide");
                    currTR.remove();
                    $("#tblCountingList").trigger("update");
                }, error: function (textStatus) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    currTR.LoadingOverlay("hide");
                    alert("Error Occurred, Please Try Again.");
                }
            });
        }
    });
    $(".btnCLEdit").click(function () {
        $("#modalCountingList").modal();
        var currCTR = $(this);
        var currTR = currCTR.closest("tr");
        var iWarehouse = currTR.find("td:eq(0)").text();
        $("#selWarehouse").val(iWarehouse);
        $("#selWarehouse").change();
        if (iWarehouse == "PD" || iWarehouse == "WH-KD" || iWarehouse == "HD" || iWarehouse == "TWH") {
            $(".divLot").show();
        } else {
            $(".divLot").hide();
        }
        $("#txtItemID").val(currTR.find("td:eq(2)").text() + " || " + currTR.find("td:eq(3)").text() + " || " + currTR.find("td:eq(1)").text());
        $("#txtDimension").val(currTR.find("td:eq(4)").text());
        $("#hfDimension").val(currTR.find("td:eq(4)").text());
        $("#txtQty").val(currTR.find("td:eq(5)").text());
        $("#txtLot").val(currTR.find("td:eq(6)").text());
        $("#txtDescription").val(currTR.find("td:eq(7)").text());
        $("#hfID").val(currCTR.data("id"));
        $("#hfItemID").val(currTR.find("td:eq(2)").text());
        $("#hfItemName").val(currTR.find("td:eq(3)").text());
        $("#hfItemProduct").val(currTR.find("td:eq(1)").text());

    });
});