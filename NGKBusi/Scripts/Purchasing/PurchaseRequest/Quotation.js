$(document).ready(function () {
    $('.tablesorter-childRow td').hide();
    $(".tblCreateQuoSelectItem").tablesorter({
        theme: "bootstrap",
        widthFixed: true,
        // widget code contained in the jquery.tablesorter.widgets.js file
        // use the zebra stripe widget if you plan on hiding any rows (filter widget)
        // the uitheme widget is NOT REQUIRED!
        widgets: ["filter", "columns", "stickyHeaders", "output"],
        widgetOptions: {
            output_delivery: 'download',
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
    $(".tblQuotationList").tablesorter({
        theme: "bootstrap",
        widthFixed: true,
        // widget code contained in the jquery.tablesorter.widgets.js file
        // use the zebra stripe widget if you plan on hiding any rows (filter widget)
        // the uitheme widget is NOT REQUIRED!
        widgets: ["filter", "columns", "stickyHeaders", "output"],
        widgetOptions: {
            output_delivery: 'download',
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
                'form-control'
            ], filter_defaultFilter: {
                // "{query} - a single or double quote signals an exact filter search
                7: '"{q}'
            }
        }
    }).tablesorterPager({
        cssGoto: '.pagenum',
        container: $(".ts-pager"),
        output: '{startRow} to {endRow} ({totalRows})',
        size: 10
    });

    $('.tablesorter').delegate('.toggle', 'click', function () {
        $(this).closest('tr').nextUntil('tr:not(.tablesorter-childRow)').find('td').toggle();
        return false;
    });

    $('.btnQuotationListDownload').click(function () {
        var $table = $(".tblQuotationList");
        var wo = $table[0].config.widgetOptions;
        wo.output_saveFileName = 'QuotationList.csv';
        $table.trigger('outputTable');
    });
    $('.btnQuotationCreateListDownload').click(function () {
        var $table = $(".tblCreateQuoSelectItem");
        var wo = $table[0].config.widgetOptions;
        wo.output_saveFileName = 'QuotationCreateList.csv';
        $table.trigger('outputTable');
    });

    $(".jqDateQuo").datepicker({ dateFormat: "dd-mm-yy", minDate: 0 });
    $(".cbQTCreateItems").change(function () {
        $(this).closest("td").find("input[type=hidden]").prop("disabled", !$(this).is(":checked"));
    });
    $(".selAddSection,.selReceivedBy").change(function () {
        var loc = location.href.split('?')[0];
        location.href = "?addNew=addNew" + ($(this).val() != "" ? "&iSection=" + $(".selAddSection").val() + "&iReceivedBy=" + $(".selReceivedBy").val() : "");
    });
    $(".tblQTVendorPrice").find("thead .thQTVendorAttachment:first .btnDelete").hide();
    var lastIDX = 1;
    $(".btnAdd").click(function () {
        var blankTHList = $(this).closest(".tblQTVendorPrice").find("thead .thQTVendorList:first").clone().attr('data-idx', ++lastIDX);
        var blankTHAttachment = $(this).closest(".tblQTVendorPrice").find("thead .thQTVendorAttachment:first").clone().attr('data-idx', lastIDX);
        var blankTHDetail = $(this).closest(".tblQTVendorPrice").find("thead .thQTVendorDetailFirst").clone().attr('data-idx', lastIDX).removeClass("thQTVendorDetailFirst");
        var blankTFPrice = $(this).closest(".tblQTVendorPrice").find("tfoot .tdQTVendorPriceTotal:first").clone().text(0).attr('data-idx', lastIDX);
        var blankTFDiscount = $(this).closest(".tblQTVendorPrice").find("tfoot .tdQTVendorDiscountTotal:first").clone().text(0).attr('data-idx', lastIDX);
        var blankTFTotal = $(this).closest(".tblQTVendorPrice").find("tfoot .tdQTVendorTotalAll:first").clone().text(0).attr('data-idx', lastIDX);


        blankTHList.find("input").val("").change();
        blankTHDetail.find(".badgeDiscount").attr("data-vendorname", "");
        blankTHAttachment.find(".ulFiles").empty().append('<li style="list-style: none; ">-</li>');
        blankTHAttachment.find(".btnAttachment").hide();
        blankTHAttachment.find(".btnDelete").show();
        bindAutoComplete(blankTHList.find(".txtThirdParty"));
        $(".trQTVendorList").append(blankTHList);
        $(".trQTVendorList").append(blankTHAttachment);
        $(".trQTVendorDetail").append(blankTHDetail);

        $(".tblQTVendorPrice tbody .trQTVendor").each(function () {
            var blankTD = $(this).find(".tdQTVendorFirst").clone().attr('data-idx', lastIDX).removeClass("tdQTVendorFirst");
            blankTD.find("input").val("0");
            blankTD.find(".spanTotal").text("0");
            blankTD.find(".rbQTVendorChoose").prop("checked", false);
            $(this).append(blankTD);
        });
        $(".trGrandTotal").append(blankTFPrice);
        $(".trGrandTotal").append(blankTFDiscount);
        $(".trGrandTotal").append(blankTFTotal);
        $(".trGrandTotal").append("<td data-idx='" + lastIDX + "'></td>");
        lastIDX++;
    });

    if ($(".txtQTVendorPrice").length > 0) {
        new AutoNumeric.multiple(".txtQTVendorPrice", { unformatOnSubmit: true, allowDecimalPadding: false });
        new AutoNumeric.multiple(".txtQTVendorDiscount", { unformatOnSubmit: true, allowDecimalPadding: false });
        new AutoNumeric.multiple(".spanCurrNetPrice", { unformatOnSubmit: true, allowDecimalPadding: false });
        new AutoNumeric.multiple(".spanCurrHistoryPrice", { unformatOnSubmit: true, allowDecimalPadding: false });
    }
    if ($(".txtQTQuantity").length > 0) {
        new AutoNumeric.multiple(".txtQTQuantity", { unformatOnSubmit: true, allowDecimalPadding: false });
    }
    $(".selUnit").select2();
    function quotationTotal(type, idx, discType = "Total") {
        var quoTotal = 0;
        switch (type.toLowerCase()) {
            case "price":
                $(".tdQTVendorPrice[data-idx='" + idx + "'] .txtQTVendorPrice").each(function () {
                    var qty = parseFloat(AutoNumeric.unformat($(this).closest("tr").find(".txtQTQuantity").val()));
                    quoTotal += qty * parseFloat(AutoNumeric.unformat($(this).val()));
                });
                break;
            case "discount":
                $(".tdQTVendorDiscount[data-idx='" + idx + "'] .txtQTVendorDiscount").each(function () {
                    var qty = parseFloat(AutoNumeric.unformat($(this).closest("tr").find(".txtQTQuantity").val()));
                    quoTotal += (discType == "Total" ? parseFloat(AutoNumeric.unformat($(this).val())) : qty * parseFloat(AutoNumeric.unformat($(this).val())));
                });
                break;
            case "total":
                $(".tdQTVendorTotal[data-idx='" + idx + "'] .hfQTVendorTotal").each(function () {
                    quoTotal += parseFloat(AutoNumeric.unformat($(this).val()));
                });
                break;
            default:
                $(".tdQTQuantity[data-idx='" + idx + "'] .txtQTQuantity").each(function () {
                    quoTotal += parseFloat(AutoNumeric.unformat($(this).val()));
                });
                break;
        }
        return quoTotal;
    }
    calculateGrandTotal();
    function calculateGrandTotal(idx, discType) {
        var setIDX = idx == null ? "" : "[data-idx='" + idx + "']";
        $(".tdQTVendorPriceTotal" + setIDX).each(function () {
            var currIDX = $(this).data("idx");
            var currTotal = AutoNumeric.format(quotationTotal("Price", currIDX, discType), { allowDecimalPadding: false });
            $(this).text(currTotal);
        });
        $(".tdQTVendorDiscountTotal" + setIDX).each(function () {
            var currIDX = $(this).data("idx");
            var currTotal = AutoNumeric.format(quotationTotal("Discount", currIDX, discType), { allowDecimalPadding: false });
            $(this).text(currTotal);
        });
        $(".tdQTVendorTotalAll" + setIDX).each(function () {
            var currIDX = $(this).data("idx");
            var currTotal = AutoNumeric.format(quotationTotal("Total", currIDX, discType), { allowDecimalPadding: false });
            $(this).text(currTotal);
        });
    }
    $(document).on("keyup", ".txtQTVendorPrice, .txtQTVendorDiscount", function () {
        console.log("keyup");
        priceTotal($(this));
        calculateRemaining($(this));
        calculateGrandTotal($(this).closest("td").data("idx"), $(this).attr("data-disctype"));
        //budgetUsageTotal();
    });
    $(document).on("keyup", ".txtQTQuantity", function () {
        var $this = $(this);
        var currTR = $this.closest("tr");
        currTR.find(".txtQTVendorPrice, .txtQTVendorDiscount").each(function () {
            priceTotal($(this));
        });
        calculateRemaining($this);
        calculateGrandTotal();
    });
    $(".txtQTVendorPrice, .txtQTVendorDiscount").each(function () {
        priceTotal($(this));
    });

    $(".btnQTVendorSubmit").click(function () {
    });
    $(document).on("change", ".rbQTVendorChoose", function () {
        $(this).closest('tr').find(".hfQTVendorChoose").val(0);
        $(this).closest('tr').find(".hfQTVendorTotal").attr("data-vendorchoose", 0);
        $(this).closest('td').find(".hfQTVendorChoose").val(1);
        $(this).closest('td').prev().find(".hfQTVendorTotal").attr("data-vendorchoose", 1);
        calculateRemaining($(this));
        //budgetUsageTotal();
    });
    //budgetUsageTotal();


    bindAutoComplete($(".txtThirdParty"));
    $(document).on("keyup", ".txtThirdParty", function (e) {
        var item = $(this).val().split('|');
        var btnAttachment = $(this).closest("th").next("th").find(".btnAttachment");
        if (item.length > 1) {
            $(this).closest("th").find(".hfThirdPartyID").val(item[0].trim());
            $(this).closest("th").find(".hfThirdPartyName").val(item[1].trim());
            btnAttachment.attr("data-thirdparty", item[1].trim());
        } else {
            $(this).closest("th").find(".hfThirdPartyID").val("");
            $(this).closest("th").find(".hfThirdPartyName").val($(this).val());
            btnAttachment.attr("data-thirdparty", $(this).val());
        }
    });
    $(document).on("change", ".txtThirdParty", function (e) {
        var idx = $(this).closest("th").data("idx");
        $(".thQTVendorDetail[data-idx='" + idx + "'] .badgeDiscount").attr("data-vendorname", $(this).val());
    });

    $(document).on("click", ".btnAttachment", function (e) {
        var thirdPartyName = $(this).attr("data-thirdparty");
        $("#hfAttachmentThirdPartyCode").val(thirdPartyName);
        var quoNumber = $("#hfQuoNumberAttachment").val();
        getQTAttachment(quoNumber, thirdPartyName, ".ulFiles");

        e.preventDefault();
    });

    getQTAttachment($("#hfQuoNumber").val(), "Quotation", ".ulQTFiles");
    function getQTAttachment(quoNumber, thirdPartyName, ulFiles) {
        var isAllowEdit = $("#hfisAllowEdit").val();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Purchasing/PurchaseRequest/getQTAttachment",
            data: {
                iQuoNumber: quoNumber,
                iThirdPartyName: thirdPartyName
            },
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {
                var liFiles = "";
                $(ulFiles).empty();
                $.each(data.files, function (k, v) {
                    switch (v.ext.toLowerCase()) {
                        case ".doc":
                        case ".docm":
                        case ".docx":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/Purchasing/PurchaseRequest/QT/" + quoNumber + "/" + thirdPartyName + "/" + v.filename + "' target='_blank'><i class='fa fa-file-word' style='color: blue; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                        case ".xls":
                        case ".xlsx":
                        case ".xlsm":
                        case ".csv":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/Purchasing/PurchaseRequest/QT/" + quoNumber + "/" + thirdPartyName + "/" + v.filename + "' target='_blank'><i class='fa fa-file-excel' style='color: green; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li>";
                            break;
                        case ".pdf":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/Purchasing/PurchaseRequest/QT/" + quoNumber + "/" + thirdPartyName + "/" + v.filename + "' target='_blank'><i class='fa fa-file-pdf' style='color: red; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                        case ".ppt":
                        case ".pptx":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/Purchasing/PurchaseRequest/QT/" + quoNumber + "/" + thirdPartyName + "/" + v.filename + "' target='_blank'><i class='fa fa-file-powerpoint' style='color: chocolate; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".gif":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/Purchasing/PurchaseRequest/QT/" + quoNumber + "/" + thirdPartyName + "/" + v.filename + "' target='_blank'><i class='fa fa-file-image' style='color: blue; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                    }
                });
                $(ulFiles).append(liFiles);
                if ($("li", ulFiles).length < 1) {
                    liFiles = "<li style = 'list-style: none; '>-</li>";
                    $(ulFiles).append(liFiles);
                }

                if (ulFiles == ".ulFiles") {
                    $(".mdlAttachment").modal();
                }

                if (!isAllowEdit) {
                    $(".ifileDelete").hide();
                }
            }, error: function (xhr, textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
            }
        });
    }

    $('.ulFiles, .ulQTFiles').on('click', '.ifileDelete', function () {
        var currLi = $(this).closest("li");
        var currID = $(this).data("id");
        if (confirm("Are you sure want to delete this file ?")) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Purchasing/PurchaseRequest/deleteQTAttachment",
                data: {
                    iID: currID
                },
                tryCount: 0,
                tryLimit: 3,
                success: function (data) {
                    currLi.remove();
                }, error: function (xhr, textStatus) {
                    if (textStatus === "timeout") {
                        this.tryCount++;
                        if (this.tryCount <= this.tryLimit) {
                            $.ajax(this);
                            return;
                        }
                    }
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    //$(".spanPriceEst").each(function () {
    //    $(this).text(AutoNumeric.format($(this).text(), {allowDecimalPadding: false }));
    //});
    $(".rbCategory").change(function () {
        bindBudget();
    });
    bindBudget();

    $(".cbQTCreateItemsAll").change(function () {
        if ($(this).is(":checked") == true) {
            $(".cbQTCreateItems:visible").prop("checked", $(this).is(":checked"));
        } else {
            $(".cbQTCreateItems").prop("checked", $(this).is(":checked"));
        }
        $(".cbQTCreateItems").change();
    });

    $(".btnCreateQuoSubmit").click(function () {
        var currAction = $(this).data("action");
        $("#formCreateQuo").attr("action", currAction);
    });

    $(document).on("click", ".btnDelete", function () {
        if (confirm('Are you sure want to remove this vendor?')) {
            var currIndex = parseInt($(this).closest("th").data("idx"));
            var deleteVendor = $(".tblQTVendorPrice th[data-idx='" + currIndex + "'],.tblQTVendorPrice td[data-idx='" + currIndex + "']")
            deleteVendor.remove();

            //var currIndex = parseInt($(this).closest("th").index());
            //var trQTVendorList = $(".tblQTVendorPrice thead .trQTVendorList th:eq(" + (currIndex - 1) + "),.tblQTVendorPrice thead .trQTVendorList th:eq(" + currIndex + ")");
            //var trQTVendorDetail = $(".tblQTVendorPrice thead .trQTVendorDetail th:eq(" + (currIndex - 2) + "),.tblQTVendorPrice thead .trQTVendorDetail th:eq(" + (currIndex - 1) + "),.tblQTVendorPrice thead .trQTVendorDetail th:eq(" + currIndex + "),.tblQTVendorPrice thead .trQTVendorDetail th:eq(" + (currIndex + 1) + "),.tblQTVendorPrice thead .trQTVendorDetail th:eq(" + (currIndex + 2) + ")");
            //$(".tblQTVendorPrice tbody .trQTVendor").each(function () {
            //    var trQTVendor = $(this).find("td:eq(" + ((currIndex * 2) - 1) + "),td:eq(" + ((currIndex * 2) - 2) + "),td:eq(" + ((currIndex * 2) - 3) + "),td:eq(" + ((currIndex * 2) - 4) + "),td:eq(" + ((currIndex * 2) - 5) + ")")
            //    trQTVendor.remove();
            //});
            //trQTVendorList.remove();
            //trQTVendorDetail.remove();
        }
    });

    $(".tblQTVendorPrice thead th,.tblQTVendorPrice tbody td").click(function () {
        //alert($(this).index());
    });

    $(document).on("click", ".badgeDiscount", function () {
        var currCTR = $(this);
        var currTH = currCTR.closest("th");
        var discountType = "Pcs";
        if (currCTR.text() == "Pcs") {
            discountType = "Total";
        } else {
            discountType = "Pcs";
        }
        var _QuoNumber = currCTR.data("quonumber");
        var _VendorName = currCTR.data("vendorname");
        var _Idx = currTH.data("idx");
        $(".tdQTVendorDiscount[data-idx='" + _Idx + "'] .txtQTVendorDiscount,.tdQTVendorPrice[data-idx='" + _Idx + "'] .txtQTVendorPrice").each(function () {
            $(this).attr("data-disctype", discountType);
        }).keyup();
        $(".tdQTVendorDiscount[data-idx='" + _Idx + "'] .hfQTVendorDiscountType").each(function () {
            $(this).val(discountType);
        });
        currCTR.text(discountType);
    });

    $(".btnQuoteCreateEdit").click(function () {
        var ID = $(this).data("id");
        var item = $(this).data("item");
        var itemID = $(this).data("itemid");
        var itemName = $(this).data("itemname");
        var itemGroup = $(this).data("itemgroup");
        $(".hfQuoteCreateID").val(ID);
        $(".txtQuoteCreateItem").val(item);
        $(".hfQuoteCreateItemID").val(itemID);
        $(".hfQuoteCreateItemName").val(itemName);
        $(".hfQuoteCreateItemGroup").val(itemGroup);
        $(".lblQuoteCreateItemGroup").text(itemGroup);
    });

    $(document).on("keyup", ".txtQuoteCreateItem", function (e) {
        var item = $(this).closest("div").find(".txtQuoteCreateItem").val().split('||');
        if (item.length > 1) {
            $(this).closest("div").find("#hfQuoteCreateItemID").val(item[0].trim());
            $(this).closest("div").find("#hfQuoteCreateItemName").val(item[1].trim());
        } else {
            $(this).closest("div").find("#hfQuoteCreateItemID").val("");
            $(this).closest("div").find("#hfQuoteCreateItemName").val($(this).val());
        }
    });

    bindQuoteCreateAutoComplete($(".txtQuoteCreateItem"));

    $(".btnQuoteSkipSign").click(function () {
        var signHeader = $(this).data("header");
        var signTitle = $(this).data("title");
        var signQuoNumber = $(this).data("quonumber");
        var signLevel = $(this).data("level");
        var signLevelSub = $(this).data("levelsub");
        $("#mdlQuoteSkipSign .lblQuoteSkipSignHeader").text(signHeader);
        $("#mdlQuoteSkipSign .lblQuoteSkipSignTitle").text(signTitle);
        $("#mdlQuoteSkipSign .tblQuoteSkipSign tbody").empty();
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Purchasing/PurchaseRequest/getSignList",
            data: {
                iQuoNumber: signQuoNumber,
                iLevel: signLevel,
                iLevelSub: signLevelSub
            },
            tryCount: 0,
            tryLimit: 3,
            success: function (data) {

                $.each(data, function (k, v) {

                    var newRow = "<tr><td>" + "<input type='checkbox' name='iSkipSignID[]' class='cbQuoteSkipSign' value='" + v.id + "'>" + "</td><td>" + v.User_NIK + "</td><td>" + v.Users.Name + "</td></tr>";

                    $("#mdlQuoteSkipSign .tblQuoteSkipSign tbody").append(newRow);
                });
                console.log(data)
            }, error: function (xhr, textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
                alert("Error Occurred, Please try again !");
            }
        });
    });
    $("#formQuotationSign").submit(function () {
        $(this).find(':submit').attr('disabled', 'disabled').text("Signing..");
    });
    $(".QTsortable").sortable({
        placeholder: "ui-state-highlight",
        handle: '.QTsortableHandle',
        cursor: "move",
        revert: true,
        start: function (event, ui) {
            // creates a temporary attribute on the element with the old index
            $(this).attr('data-previndex', ui.item.index());
        },
        update: function (event, ui) {
            $(".QTsortable tr").each(function (index, element) {
                var seq = index + 1;
                $(this).attr("data-sequence", seq);
                $(this).find(".hfQTVendorLineSequence").val(seq);
            });
        }
    });

    $(document).on("click", ".btnQTLineDelete", function () {
        var $btn = $(this);
        var lineID = $btn.data("lineid");
        var $tr = $btn.closest(".trQTVendor");
        if (confirm('Are you sure want to remove this line?')) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Purchasing/PurchaseRequest/QuotationLineDelete",
                data: {
                    iQuoNumber: $("#hfQuoNumber").val(),
                    iLineID: lineID
                },
                success: function (data) {
                    $tr.remove();
                    $(".QTsortable tr").each(function (index, element) {
                        var seq = index + 1;
                        $(this).attr("data-sequence", seq);
                        $(this).find(".hfQTVendorLineSequence").val(seq);
                    });
                    calculateGrandTotal();
                },
                error: function () {
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });

    $(document).on('click', '.btnBudgetDetails', function () {
        var budgetNo = $(this).data('budgetno');
        var items = [];
        var grandTotal = 0;
        $('.tblQTVendorPrice tbody .trQTVendor').each(function () {
            var rowBudgetNo = $(this).data('budgetno');
            if (rowBudgetNo && rowBudgetNo.split('|')[0] === budgetNo) {
                var item = {
                    item: '',
                    unit: '',
                    qty: 0,
                    price: 0,
                    total: 0,
                    note: ''
                };
                // Extract item name
                var labelItem = $(this).find('label.font-weight-bold:contains("Item:")');
                if (labelItem.length > 0) {
                    item.item = labelItem[0].nextSibling ? labelItem[0].nextSibling.textContent.trim() : '';
                }
                // Extract unit and qty
                item.unit = $(this).find('td:eq(1)').text().trim();
                var qtyText = $(this).find('td:eq(2)').text().trim();
                item.qty = parseFloat(qtyText.replace(/,/g, '')) || 0;
                // Extract net price from spanCurrNetPrice
                var netPriceText = $(this).find('.spanCurrNetPrice').text().replace(/,/g, '').trim();
                item.price = parseFloat(netPriceText) || 0;
                // Calculate total
                item.total = item.qty * item.price;
                grandTotal += item.total;
                // Extract note
                var labelNote = $(this).find('label.font-weight-bold:contains("Note:")');
                if (labelNote.length > 0) {
                    item.note = labelNote[0].nextSibling ? labelNote[0].nextSibling.textContent.trim() : '';
                }
                items.push(item);
            }
        });
        var $tbody = $('#budgetItemsTable tbody');
        $tbody.empty();
        function formatNumber(num) {
            return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
        if (items.length > 0) {
            var rows = items.map(function (item) {
                return '<tr>' +
                    '<td>' + item.item + '</td>' +
                    '<td>' + item.unit + '</td>' +
                    '<td class="text-right">' + formatNumber(item.qty) + '</td>' +
                    '<td class="text-right">' + formatNumber(item.price) + '</td>' +
                    '<td class="text-right">' + formatNumber(item.total) + '</td>' +
                    '<td>' + item.note + '</td>' +
                    '</tr>';
            });
            $tbody.append(rows.join(''));
        } else {
            $tbody.append('<tr><td colspan="6">No items found for this budget.</td></tr>');
        }
        $('#budgetItemsGrandTotal').text(formatNumber(grandTotal));
        $('#budgetDetailsModal').modal('show');
    });

});

function bindQuoteCreateAutoComplete(e) {
    e.autocomplete({
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Purchasing/PurchaseRequest/GetItemID",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iItemGroup: e.closest("div").find(".hfQuoteCreateItemGroup").val(),
                    iItemID: e.val()
                },
                success: function (data) {
                    response(data);
                }, error: function (xhr, textStatus) {
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
        minLength: 1
        , select: function (event, ui) {
            $(this).closest("div").find("#hfQuoteCreateItemID").val(ui.item.itemID);
            $(this).closest("div").find("#hfQuoteCreateItemName").val(ui.item.itemName);
        }
    });
}

function bindBudget() {
    var budgetList = [];
    $(".tblQTVendorPrice tbody .trQTVendor").each(function () {
        if ($.inArray($(this).data("budgetno"), budgetList) < 0) {
            budgetList.push($(this).data("budgetno"));
        }
    });
    $(".tblPRBudgetRemaining tbody").empty();
    budgetList.forEach(function (e) {
        var bNumber = e.split("|")[0];
        var bName = e.split("|")[1];
        $.ajax({
            type: "POST",
            url: "/NGKBusi/Purchasing/PurchaseRequest/GetRemainingBudget",
            dataType: "json",
            tryCount: 0,
            tryLimit: 3,
            data: {
                iCategory: $(".rbCategory:checked").val(),
                iBudgetNumber: bNumber,
                iBudgetName: bName,
                iQuoID: $("#hfQuoID").val()
            },
            success: function (data) {
                var estimatePrice = 0.00;
                $(".tblQTVendorPrice tbody .trQTVendor[data-budgetno='" + bNumber + "|" + bName + "'").each(function () {
                    var selectedPrice = $(this).find(".hfQTVendorChoose[value='1']") || $(this).find(".hfQTVendorChoose:first");
                    var vendorPrice = selectedPrice.closest("td").prev().prev().prev().find(".txtQTVendorPrice").val() || 0.00;
                    var vendorDiscount = selectedPrice.closest("td").prev().prev().find(".txtQTVendorDiscount").val() || 0.00;
                    var vendorDiscountType = selectedPrice.closest("td").prev().prev().find(".txtQTVendorDiscount").data("disctype");
                    var vendorTotal = parseFloat(AutoNumeric.unformat(vendorPrice)) * parseFloat(AutoNumeric.unformat($(this).find(".txtQTQuantity").val()));
                    estimatePrice += vendorTotal - (vendorDiscountType == "Total" ? parseFloat(AutoNumeric.unformat(vendorDiscount)) : parseFloat(AutoNumeric.unformat(vendorDiscount) * AutoNumeric.unformat($(this).find(".txtQTQuantity").val())));
                });

                var budgetTotal = data.Total_Amount || 0.00;
                var budgetUsage = data.Usage || 0.00;
                var budgetRemaining = budgetTotal - budgetUsage - estimatePrice;
                var budgetStatus = (bNumber == "Rebate" || bNumber == "New-Price" || bNumber == "Selling-Out" || bNumber == "Next-FY-Budget") || budgetRemaining >= 0 ? " <span style='color:green;'>(On Budget <i class='fa fa-check'></i>)</span>" : " <span style='color:red;'>(Over Budget <i class='fa fa-triangle-exclamation'></i>)</span>";
                budgetStatus = bNumber == "New-Price" || bNumber == "Selling-Out" || bNumber == "Next-FY-Budget" ? "" : budgetStatus;
                var newRow = "<tr>";
                newRow += "<td class='tdPRRemainingBudget' data-budgetno='" + bNumber + "'>" + bNumber + " | " + bName + " <label class='lblBudgetStatus'>" + budgetStatus + "</label>" + "</td>";
                newRow += "<td class='tdPRRemainingAmount'>" + (bNumber == "Rebate" || bNumber == "New-Price" || bNumber == "Selling-Out" || bNumber == "Next-FY-Budget" ? "-" : AutoNumeric.format(budgetTotal, { allowDecimalPadding: false })) + "</td>";
                newRow += "<td class='tdPRRemainingInUse'>" + (bNumber == "Rebate" || bNumber == "New-Price" || bNumber == "Selling-Out" || bNumber == "Next-FY-Budget" ? "-" : AutoNumeric.format(budgetUsage, { allowDecimalPadding: false })) + "</td>";
                newRow += "<td class='tdPRRemainingEstimation' data-remaining='" + (budgetTotal - budgetUsage) + "'>" + AutoNumeric.format(estimatePrice, { allowDecimalPadding: false }) + "</td>";
                newRow += "<td class='tdPRRemainingTotal'>" + (bNumber == "Rebate" || bNumber == "New-Price" || bNumber == "Selling-Out" || bNumber == "Next-FY-Budget" ? "-" : AutoNumeric.format(budgetRemaining, { allowDecimalPadding: false })) + "</td>";
                //newRow += "<td><button type='button' class='btn btn-info btn-sm btnBudgetDetails' data-budgetno='" + bNumber + "'>Details</button></td>";
                newRow += "</tr>";
                $(".tblPRBudgetRemaining tbody").append(newRow);
            }, error: function (xhr, textStatus) {
                if (textStatus === "timeout") {
                    this.tryCount++;
                    if (this.tryCount <= this.tryLimit) {
                        $.ajax(this);
                        return;
                    }
                }
            }
        });
    });
}

function calculateRemaining(e) {
    var estimatePrice = 0.00;
    var currBudgetNo = e.closest("tr").data("budgetno");
    $(".tblQTVendorPrice tbody .trQTVendor[data-budgetno='" + currBudgetNo + "'").each(function () {
        var selectedPrice = $(this).find(".hfQTVendorChoose[value='1']") || $(this).find(".hfQTVendorChoose:first");
        var vendorPrice = selectedPrice.closest("td").prev().prev().prev().find(".txtQTVendorPrice").val() || 0.00;
        var vendorDiscount = selectedPrice.closest("td").prev().prev().find(".txtQTVendorDiscount").val() || 0.00;
        var vendorDiscountType = selectedPrice.closest("td").prev().prev().find(".txtQTVendorDiscount").data("disctype");
        var vendorTotal = parseFloat(AutoNumeric.unformat(vendorPrice)) * parseFloat(AutoNumeric.unformat($(this).find(".txtQTQuantity").val()));
        estimatePrice += vendorTotal - (vendorDiscountType == "Total" ? parseFloat(AutoNumeric.unformat(vendorDiscount)) : parseFloat(AutoNumeric.unformat(vendorDiscount) * AutoNumeric.unformat($(this).find(".txtQTQuantity").val())));
    });
    var budgetRemainingTR = $(".tblPRBudgetRemaining tbody tr:has(td[data-budgetno='" + currBudgetNo.split("|")[0] + "'])");
    var budgetRemainingTotalTD = budgetRemainingTR.find(".tdPRRemainingTotal");
    var budgetEstimationTD = budgetRemainingTR.find(".tdPRRemainingEstimation");
    var budgetRemaining = budgetEstimationTD.data("remaining");
    var budgetRemainingTotal = parseFloat(AutoNumeric.unformat(budgetRemaining)) - parseFloat(AutoNumeric.unformat(estimatePrice));
    var budgetStatus = (currBudgetNo.split("|")[0] == "Rebate" || currBudgetNo.split("|")[0] == "New-Price" || currBudgetNo.split("|")[0] == "Selling-Out" || currBudgetNo.split("|")[0] == "Next-FY-Budget") || budgetRemainingTotal >= 0 ? " <span style='color:green;'>(On Budget <i class='fa fa-check'></i>)</span>" : " <span style='color:red;'>(Over Budget <i class='fa fa-triangle-exclamation'></i>)</span>";
    if (currBudgetNo.split("|")[0] == "Rebate" || currBudgetNo.split("|")[0] == "New-Price" || currBudgetNo.split("|")[0] == "Selling-Out" || currBudgetNo.split("|")[0] == "Next-FY-Budget") {
        budgetRemainingTotalTD.text('-');
        budgetRemainingTR.find(".tdPRRemainingAmount").text('-');
    } else {
        budgetRemainingTotalTD.text(AutoNumeric.format(budgetRemainingTotal, { allowDecimalPadding: false }));
    }
    budgetRemainingTR.find(".lblBudgetStatus").html(budgetStatus);
    budgetEstimationTD.text(AutoNumeric.format(estimatePrice, { allowDecimalPadding: false }));
}

function priceTotal($this) {
    var input = $this.val();
    var currVal = AutoNumeric.format(input, { allowDecimalPadding: false });
    var currTR = $this.closest('tr');
    var currQty = parseFloat(AutoNumeric.unformat(currTR.find(".txtQTQuantity").val())) || 0;
    var currPrice = $this.hasClass("txtQTVendorDiscount") ? $this.closest("td").prev().find(".txtQTVendorPrice").val() : $this.val();
    var currDiscount = $this.hasClass("txtQTVendorPrice") ? $this.closest("td").next().find(".txtQTVendorDiscount").val() : $this.val();
    var currDiscountType = $this.hasClass("txtQTVendorPrice") ? $this.closest("td").next().find(".txtQTVendorDiscount").attr("data-disctype") : $this.attr("data-disctype");
    var currTotal = (AutoNumeric.unformat(currPrice) * currQty) - (currDiscountType == "Total" ? AutoNumeric.unformat(currDiscount) : AutoNumeric.unformat(currDiscount) * currQty);
    var currHFTotal = $this.hasClass("txtQTVendorDiscount") ? $this.closest("td").next().find(".hfQTVendorTotal") : $this.closest("td").next().next().find(".hfQTVendorTotal");
    var currSpanTotal = $this.hasClass("txtQTVendorDiscount") ? $this.closest("td").next().find(".spanTotal") : $this.closest("td").next().next().find(".spanTotal");
    currHFTotal.val(currTotal);
    currSpanTotal.text(AutoNumeric.format(currTotal ?? 0, { allowDecimalPadding: false }));
}

function bindAutoComplete(e) {
    e.autocomplete({
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/Purchasing/PurchaseRequest/GetThirdParty",
                dataType: "json",
                tryCount: 0,
                tryLimit: 3,
                data: {
                    iThirdParty: e.val()
                },
                success: function (data) {
                    response(data);
                }, error: function (xhr, textStatus) {
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
        minLength: 1
        , select: function (event, ui) {
            $(this).closest("th").find(".hfThirdPartyID").val(ui.item.thirdPartyID);
            $(this).closest("th").find(".hfThirdPartyName").val(ui.item.thirdPartyName);
            $(this).closest("th").next("th").find(".btnAttachment").attr("data-thirdparty", ui.item.thirdPartyName);
        }
    });

    $("#selPRFilterStatus,#selPRFilterYear,#selPRFilterLevel").change(function () {
        $(".btnPRFilter").click();
    });

    $('#QTComments').comments({
        enableNavigation: false,
        enablePinging: false,
        enableAttachments: true,
        enableUpvoting: false,
        timeFormatter: function (time) {
            return moment(time).format("MMMM D, YYYY [at] HH:mm");
        },
        postComment: function (commentJSON, success, error) {
            console.log("postComment");
            commentJSON["QuoNumber"] = $("#hfQuoNumber").val();
            commentJSON["fullname"] = "@(currUserFullName)";
            commentJSON["nik"] = "@(currUserID)";

            //NOTE!! :
            //If updating the comment js file don't forget to change :
            //jquery-comment-min.js "return e.attachments.length>0" into "return e.attachments?.length>0"
            //jquery-comment.js "return commentModel.attachments.length" into "return commentModel.attachments?.length"
            //to avoid error posting without attachment
            var formData = new FormData();
            $.each(Object.keys(commentJSON), function (index, key) {
                var value = commentJSON[key];
                if (key != 'attachments') {
                    formData.append(key, value);
                } else {
                    $.each(value, function (i, v) {
                        formData.append(key, v.file);
                    });
                    //console.log(value)
                }
            });

            //var attachmentsToBeCreated = commentJSON.attachments.filter(function (attachment) {
            //    return !attachment.id
            //});
            //$(attachmentsToBeCreated).each(function (index, attachment) {
            //    formData.append('attachments_to_be_created', attachment.file);
            //});

            $.ajax({
                type: 'post',
                url: "/NGKBusi/Purchasing/PurchaseRequest/QuotationCommentAdd",
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                success: function (comment) {
                    success(comment)
                },
                error: error
            });
            console.log(commentJSON);
        },
        putComment: function (commentJSON, success, error) {
            console.log("putComment");
            commentJSON["QuoNumber"] = $("#hfQuoNumber").val();
            commentJSON["fullname"] = "@(currUserFullName)";
            commentJSON["nik"] = "@(currUserID)";

            var formData = new FormData();
            $.each(Object.keys(commentJSON), function (index, key) {
                var value = commentJSON[key];
                if (key != 'attachments') {
                    formData.append(key, value);
                } else {
                    $.each(value, function (i, v) {
                        formData.append(key, v.file);
                    });
                    //console.log(value)
                }
            });

            //var attachmentsToBeCreated = commentJSON.attachments.filter(function (attachment) {
            //    return !attachment.id
            //});
            //$(attachmentsToBeCreated).each(function (index, attachment) {
            //    formData.append('attachments_to_be_created', attachment.file);
            //});

            $.ajax({
                type: 'post',
                url: "/NGKBusi/Purchasing/PurchaseRequest/QuotationCommentEdit",
                data: formData,
                cache: false,
                contentType: false,
                processData: false,
                success: function (comment) {
                    success(comment)
                },
                error: error
            });
            console.log(commentJSON);
        },
        deleteComment: function (commentJSON, success, error) {
            console.log("deleteComment");
            commentJSON["QuoNumber"] = $("#hfQuoNumber").val();
            console.log(commentJSON);
            $.ajax({
                type: 'post',
                url: "/NGKBusi/Purchasing/PurchaseRequest/QuotationCommentDelete",
                data: commentJSON,
                success: success,
                error: error
            });
        }, getComments: function (success, error) {
            $.ajax({
                type: 'get',
                url: "/NGKBusi/Purchasing/PurchaseRequest/QuotationCommentGet",
                contentType: 'multipart/form-data',
                data: {
                    iQuoNumber: $("#hfQuoNumber").val(),
                    iNIK: "@(currUserID)"
                },
                success: function (commentsArray) {
                    success(commentsArray)
                },
                error: error
            });
        }
    });

}