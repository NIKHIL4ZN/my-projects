
const {
    calculateEstimatedTax
} = require("../src/utils/taxCalculator");

describe("Tax Calculator", () => {

    test("returns zero values when income is zero", () => {
        const result = calculateEstimatedTax(0);

        expect(result).toEqual({
            grossIncome: 0,
            estimatedTax: 0,
            estimatedNetIncome: 0,
            effectiveTaxRate: 0
        });
    });

    test("calculates 15% tax on R5,000 income", () => {
        const result = calculateEstimatedTax(5000);

        expect(result.grossIncome).toBe(5000);
        expect(result.estimatedTax).toBe(750);
        expect(result.estimatedNetIncome).toBe(4250);
        expect(result.effectiveTaxRate).toBe(15);
    });

    test("calculates 15% tax on R10,000 income", () => {
        const result = calculateEstimatedTax(10000);

        expect(result.grossIncome).toBe(10000);
        expect(result.estimatedTax).toBe(1500);
        expect(result.estimatedNetIncome).toBe(8500);
        expect(result.effectiveTaxRate).toBe(15);
    });

    test("calculates 15% tax on R20,000 income", () => {
        const result = calculateEstimatedTax(20000);

        expect(result.grossIncome).toBe(20000);
        expect(result.estimatedTax).toBe(3000);
        expect(result.estimatedNetIncome).toBe(17000);
        expect(result.effectiveTaxRate).toBe(15);
    });

    test("calculates 15% tax on R30,000 income", () => {
        const result = calculateEstimatedTax(30000);

        expect(result.grossIncome).toBe(30000);
        expect(result.estimatedTax).toBe(4500);
        expect(result.estimatedNetIncome).toBe(25500);
        expect(result.effectiveTaxRate).toBe(15);
    });

    test("calculates 15% tax on R40,000 income", () => {
        const result = calculateEstimatedTax(40000);

        expect(result.grossIncome).toBe(40000);
        expect(result.estimatedTax).toBe(6000);
        expect(result.estimatedNetIncome).toBe(34000);
        expect(result.effectiveTaxRate).toBe(15);
    });

    test("handles invalid income safely", () => {
        const result = calculateEstimatedTax("invalid");

        expect(result).toEqual({
            grossIncome: 0,
            estimatedTax: 0,
            estimatedNetIncome: 0,
            effectiveTaxRate: 0
        });
    });
});
