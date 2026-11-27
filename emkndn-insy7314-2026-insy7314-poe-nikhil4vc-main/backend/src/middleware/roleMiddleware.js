function requireRole(...allowedRoles) {
    return (req, res, next) => {
        if (!req.user) {
            return res.status(401).json({
                success: false,
                message: "Authentication required."
            });
        }

        if (!allowedRoles.includes(req.user.role)) {
            return res.status(403).json({
                success: false,
                message: "You do not have permission to access this resource."
            });
        }

        next();
    };
}

module.exports = {
    requireRole
};

// Code attributes

/*
Title: Implementing Role-Based Access Control (RBAC) in Node.js
Author(s): Suneel Kumar
Date: 2023
Version: Not applicable
Type: Medium article
Availability: https://medium.com/@techsuneel99/implementing-role-based-access-control-rbac-in-node-js-871591b80a83
*/