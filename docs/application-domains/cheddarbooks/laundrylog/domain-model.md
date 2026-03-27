# CheddarBooks LaundryLog Domain Model

This note captures the current domain language and the core functional requirements.

## Core Domain Language

The current domain language should prefer:

- command: `LogLaundryExpense`
- event: `LaundryExpenseLogged`

The current understanding is that:

- the expense is the key business fact
- washer, dryer, and supplies are expense types, not separate event kinds
- session context is useful in the UI and workflow, but is not more important than the expense fact itself

## Functional Requirements

LaundryLog must support:

1. setting or confirming a laundry location
2. logging expenses by type:
   - washer
   - dryer
   - supplies
3. entering quantity
4. entering unit price
5. calculating entry total from quantity and unit price
6. capturing payment method
7. showing running session total
8. logging multiple entries during one laundry outing

## UX Pressure On The Domain Model

The domain model should stay compatible with:

- low-friction mobile entry
- repeated entry within one outing
- enough detail for later reporting
- a UI that can show session context without making session context the primary business fact
