/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.4/15.4.4/15.4.4.19/15.4.4.19-9-9.js
 * @description Array.prototype.map - empty array to be returned if 'length' is 0 (subclassed Array, length overridden to '0' (type conversion))
 */


function testcase() {

        var accessed = false;

        function callbackfn(val, idx, obj) {
            accessed = true;
            return val > 10;
        }

        var Foo = function () { };
        Foo.prototype = [1, 2, 3];
        var obj = new Foo();
        obj.length = '0';

        var testResult = Array.prototype.map.call(obj, callbackfn);
        return testResult.length === 0;

    }
runTestCase(testcase);
